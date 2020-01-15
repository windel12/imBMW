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
        public static Thread ReadinessThread = new Thread(CheckStatus);

        private HttpRequestCommand pauseCommand = new HttpRequestCommand("commands/?cmd=pause", 500);
        private HttpRequestCommand nextCommand = new HttpRequestCommand("commands/?cmd=next", response =>
        {
            InstrumentClusterElectronics.ShowNormalTextWithoutGong(response);
            Bordmonitor.ShowText(response, BordmonitorFields.Title);
        });

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

            ReadinessThread.Start();
        }

        private static void ProcessCommand(object o)
        {
            var httpRequestCommand = (HttpRequestCommand) o;
            string result = Execute(httpRequestCommand.Param);
            if (httpRequestCommand.Callback != null)
            {
                httpRequestCommand.Callback(result);
            }
            if (httpRequestCommand.AfterSendTimeout > 0)
            {
                Thread.Sleep(httpRequestCommand.AfterSendTimeout);
            }
        }

        private static string Execute(string param)
        {
            HttpWebRequest request = WebRequest.Create(path + param) as HttpWebRequest;
            request.Timeout = 3000;
            request.KeepAlive = false;
            HttpWebResponse response = null;
            try
            {
                Logger.Trace("Sending request: " + path );
                response = request?.GetResponse() as HttpWebResponse;
                Logger.Trace("Responded successfull.");
                using (var stream = response?.GetResponseStream())
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    var result = ASCIIEncoding.GetString(bytes, 0, bytes.Length);
                    return result;
                }
            }
            catch (Exception ex)
            {
                if (param == "ping")
                    throw;

                var webException = ex as WebException;
                var status = webException != null ? (int) webException.Status : -1;
                Logger.Error(ex, "status: " + status);
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
                if (ReadinessThread.ThreadState == ThreadState.Suspended || ReadinessThread.ThreadState == ThreadState.SuspendRequested)
                {
                    ReadinessThread.Resume();
                }
            }));
        }

        public static void Shutdown()
        {
            commands.Enqueue(new HttpRequestCommand("shutdown"));
        }

        public override void Play()
        {
            SetPlaying(true);
            commands.Enqueue(new HttpRequestCommand("commands/?cmd=play"));
        }

        public override void Pause()
        {
            SetPlaying(false);
            commands.Enqueue(pauseCommand);
        }

        public override void Next()
        {
            commands.Enqueue(pauseCommand);
            commands.Enqueue(nextCommand);
        }

        public override void Prev()
        {
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
                    Ready = true;
                    FrontDisplay.RefreshLEDs(LedType.Green);
                    ReadinessThread.Suspend();
                }
                catch (Exception ex)
                {
                    FrontDisplay.RefreshLEDs(LedType.OrangeBlinking, append: true);
                    Logger.Info("CheckStatus: Volumio isn't ready yet.");
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
