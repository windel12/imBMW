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
        public static bool Ready = false;
        public static Thread CheckStatusThread = new Thread(CheckStatus);

        public VolumioRestApiPlayer(Cpu.Pin chipSelect, Cpu.Pin externalInterrupt, Cpu.Pin reset)
        {
            commands = new QueueThreadWorker(ProcessCommand, "httpRequestsThread", ThreadPriority.Lowest);

            var netif = new EthernetENC28J60(SPI.SPI_module.SPI2, chipSelect, externalInterrupt/*, reset*/);
            netif.Open();
            ////netif.EnableDhcp();
            ////netif.EnableDynamicDns();
            netif.EnableStaticIP("169.254.194.93", "255.255.0.0", "0.0.0.0");
            int attemps = 0;
            while (netif.IPAddress != "169.254.194.93" && attemps++ < 4)
            {
                System.Threading.Thread.Sleep(250);
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
                    stream.Read(bytes, 0, bytes.Length);
                    //string text = ASCIIEncoding.GetString(bytes, 0, bytes.Length);
                    string text = new string(Encoding.UTF8.GetChars(bytes));
                    Logger.Trace("Responded successfull. Bytes: " + bytes.ToHex(' ') + ". Text: " + text);
                    return text;
                    //string[] values = StringHelpers.Split(text, " - ");
                    //return values.Length > 1 ? values[1] : text;
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
                Logger.Warning("REBOOTED.");
                if (CheckStatusThread.ThreadState == ThreadState.Suspended || CheckStatusThread.ThreadState == ThreadState.SuspendRequested)
                {
                    CheckStatusThread.Resume();
                }
            }));
        }

        public static void Shutdown()
        {
            commands.Enqueue(new HttpRequestCommand("shutdown"));
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
                    FrontDisplay.RefreshLEDs(LedType.OrangeBlinking, append: true);
                    Logger.Trace("CheckStatus: Volumio isn't ready yet.");
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
