using System.Net;
using System.Text;
using System;
using System.Threading;
using Microsoft.SPOT.Hardware;
using GHI.Networking;
using imBMW.Multimedia;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;
using imBMW.Features.Multimedia.Models;
using imBMW.Enums;
using Json.NETMF;
using System.Collections;

namespace imBMW.Features.Multimedia
{
    public class VolumioRestApiPlayer : AudioPlayerBase
    {
        private static QueueThreadWorker commands;

        private static string path = "http://169.254.194.94/api/v1/";
        private string netifIpAddress = "169.254.194.93";
        private int _waitIpAddressAttempts = 8;

        public static Thread CheckStatusThread = new Thread(CheckStatus);

        public VolumioRestApiPlayer(Cpu.Pin chipSelect, Cpu.Pin externalInterrupt, Cpu.Pin reset)
        {
            commands = new QueueThreadWorker(ProcessCommand, "httpRequestsThread", ThreadPriority.Lowest);

            var netif = new EthernetENC28J60(SPI.SPI_module.SPI2, chipSelect, externalInterrupt/*, reset*/);
            netif.Open();
            ////netif.EnableDhcp();
            ////netif.EnableDynamicDns();
            netif.EnableStaticIP(netifIpAddress, "255.255.0.0", "0.0.0.0");
            int attemps = 0;
            while (netif.IPAddress != netifIpAddress && ++attemps < _waitIpAddressAttempts)
            {
                System.Threading.Thread.Sleep(250);
            }
            if (attemps >= _waitIpAddressAttempts)
            {
                Logger.Error("IP address not acquired! Current IP address: " + netif.IPAddress);
            }
            Inited = true;

            CheckStatusThread.Start();
        }

        private static void ProcessCommand(object o)
        {
            var httpRequestCommand = (HttpRequestCommand) o;
            Execute(httpRequestCommand);
        }

        private static void Execute(HttpRequestCommand httpRequestCommand)
        { 
            string fullPath = path + httpRequestCommand.Param;
            HttpWebRequest request = WebRequest.Create(fullPath) as HttpWebRequest;
            request.Timeout = 3000;
            request.ReadWriteTimeout = 3000;
            request.KeepAlive = false;
            HttpWebResponse response = null;
            string responseText = "";
            try
            {
                Logger.Trace("Sending request: " + fullPath);

#if OnBoardMonitorEmulator
                responseText = OnBoardMonitorEmulator.DevicesEmulation.VolumioEmulator.MakeHttpRequest(httpRequestCommand.Param);
                return;
#endif
                response = request?.GetResponse() as HttpWebResponse;
                using (var stream = response?.GetResponseStream())
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.ReadTimeout = 3000;
                    stream.Read(bytes, 0, bytes.Length);
                    responseText = new string(Encoding.UTF8.GetChars(bytes));
                    Logger.Trace("Responded successfull. ResponseText: " + responseText);
                }
            }
            catch (Exception ex)
            {
                if (httpRequestCommand.ErrorCallback == null)
                {
                    var webException = ex as WebException;
                    var status = webException != null ? (int) webException.Status : -1;
                    throw new Exception("WebExStatus: " + status, ex);
                }
                else
                {
                    httpRequestCommand.ErrorCallback(ex);
                }
            }
            finally
            {
                if (httpRequestCommand.SuccessCallback != null)
                {
                    httpRequestCommand.SuccessCallback(responseText);
                }

                response?.Dispose();
#if NETMF
                request?.Dispose();
#endif
            }
        }

        public static void Reboot()
        {
            commands.Enqueue(new HttpRequestCommand("reboot", response =>
            {
				Thread.Sleep(500);
                Logger.Warning(response);
                Thread.Sleep(2500);
                if (CheckStatusThread.ThreadState == ThreadState.Suspended || CheckStatusThread.ThreadState == ThreadState.SuspendRequested)
                {
                    CheckStatusThread.Resume();
                }
            }));
        }

        public static void Shutdown(ActionString successCallback, ActionException errorCallback = null)
        {
            commands.Enqueue(new HttpRequestCommand("shutdown", successCallback, errorCallback));
        }

        public override void Play()
        {
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=play", response =>
            {
                Hashtable result = (Hashtable)JsonParser.JsonDecode(response);
                InstrumentClusterElectronics.ShowNormalTextWithoutGong(result["response"].ToString(), timeout: 5000);
                IsPlaying = true;
            }));
        }

        public override void Pause()
        {
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=pause", response => IsPlaying = false));
        }

        public override void Next()
        {
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=pause", response =>
            {
                Thread.Sleep(Settings.Instance.Delay1);
                DigitalSignalProcessingAudioAmplifier.ChangeSource(AudioSource.TunerTape);
                Thread.Sleep(Settings.Instance.Delay2);
            }));
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=next", response =>
            {
                OnTrackChanged(response);

                Thread.Sleep(Settings.Instance.Delay3);
                DigitalSignalProcessingAudioAmplifier.ChangeSource(AudioSource.CD);
                Thread.Sleep(Settings.Instance.Delay4);
                DigitalSignalProcessingAudioAmplifier.ChangeSource(AudioSource.CD);
            }));
        }

        public override void Prev()
        {
            //commands.Enqueue(new HttpRequestCommand("commands/?cmd=stop", x => Thread.Sleep(500)));
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=prev"));
        }

        public override string ChangeTrackTo(string fileName)
        {
            return "";
        }

        public override bool RandomToggle(byte diskNumber)
        {
            return true;
        }

        public static void CheckStatus()
        {
            while (true)
            {
                try
                {
                    var pingCommand = new HttpRequestCommand("ping");
                    Execute(pingCommand);
                    Logger.Trace("CheckStatus: Volumio READY!");
                    InstrumentClusterElectronics.ShowNormalTextWithGong("Volumio READY!");
                    FrontDisplay.RefreshLEDs(LedType.Green);
                    CheckStatusThread.Suspend();
                }
                catch (Exception ex)
                {
                    if ((FrontDisplay.CurrentLEDState & LedType.Orange) != 0)
                    {
                        FrontDisplay.RefreshLEDs(LedType.Orange, remove: true);
                    }
                    else
                    {
                        FrontDisplay.RefreshLEDs(LedType.Orange, append: true);
                    }
                    Logger.Trace("CheckStatus: Volumio isn't ready yet.");
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
