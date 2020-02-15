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
            string result = Execute(httpRequestCommand.Param);
            if (httpRequestCommand.Callback != null)
            {
                httpRequestCommand.Callback(result);
            }
        }

        private static string Execute(string param)
        {
            string fullPath = path + param;
            HttpWebRequest request = WebRequest.Create(fullPath) as HttpWebRequest;
            request.Timeout = 3000;
            request.ReadWriteTimeout = 3000;
            request.KeepAlive = false;
            HttpWebResponse response = null;
            try
            {
                Logger.Trace("Sending request: " + fullPath);

#if OnBoardMonitorEmulator
                return OnBoardMonitorEmulator.DevicesEmulation.VolumioEmulator.MakeHttpRequest(param);
#endif
                response = request?.GetResponse() as HttpWebResponse;
                using (var stream = response?.GetResponseStream())
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.ReadTimeout = 3000;
                    stream.Read(bytes, 0, bytes.Length);
                    string text = new string(Encoding.UTF8.GetChars(bytes));
                    Logger.Trace("Responded successfull. Text: " + text);
                    return text;
                }
            }
            catch (Exception ex)
            {
                var webException = ex as WebException;
                var status = webException != null ? (int) webException.Status : -1;
                throw new Exception("WebExStatus: " + status, ex);
            }
            finally
            {
                response?.Dispose();
#if NETMF
                request?.Dispose();
#endif
            }
            return string.Empty;
        }

        public static void Reboot()
        {
            commands.Enqueue(new HttpRequestCommand("reboot", response =>
            {
                Thread.Sleep(3000);
                Logger.Warning("REBOOTED!");
                if (CheckStatusThread.ThreadState == ThreadState.Suspended || CheckStatusThread.ThreadState == ThreadState.SuspendRequested)
                {
                    CheckStatusThread.Resume();
                }
            }));
        }

        public static void Shutdown()
        {
            commands.Enqueue(new HttpRequestCommand("shutdown", response =>
            {
                Thread.Sleep(2000);
                Logger.Warning("SHUTTEDDOWN!");
            }));
        }

        public override void Play()
        {
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=play", x => IsPlaying = true));
        }

        public override void Pause()
        {
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=pause", x => IsPlaying = false));
        }

        public override void Next()
        {
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=stop", x => Thread.Sleep(500)));
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=next", OnTrackChanged));
        }

        public override void Prev()
        {
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=stop", x => Thread.Sleep(500)));
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
                    Execute("ping");
                    Logger.Trace("CheckStatus: Volumio READY!");
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
