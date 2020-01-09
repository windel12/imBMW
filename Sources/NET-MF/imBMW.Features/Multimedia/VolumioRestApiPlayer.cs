using imBMW.Multimedia;
using Microsoft.SPOT.Hardware;
using GHI.Networking;
using System.Net;
using imBMW.Tools;
using System.Text;
using System;

namespace imBMW.Features.Multimedia
{
    public class VolumioRestApiPlayer : AudioPlayerBase
    {
        private static string path = "http://169.254.194.94/api/v1/";

        public VolumioRestApiPlayer(Cpu.Pin chipSelect, Cpu.Pin externalInterrupt, Cpu.Pin reset)
        {
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
        }

        public override bool Inited
        {
            get;set;
        }

        public override bool IsPlaying
        {
            get; protected set;
        }

        public static string Execute(string param)
        {
            string result = string.Empty;
            var request = WebRequest.Create(path + param) as HttpWebRequest;
            request.Timeout = 3000;
            HttpWebResponse response = null;
            try
            {
                response = request?.GetResponse() as HttpWebResponse;
                using (var stream = response?.GetResponseStream())
                {
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    result = ASCIIEncoding.GetString(bytes, 0, bytes.Length);//.ASCIIToUTF8();
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex, "REST:" + param + "failed");
            }
            finally
            {
                response?.Dispose();
#if NETMF
                request?.Dispose();
#endif
            }
            return result;
        }

        public static void Reboot()
        {
            Execute("reboot");
        }

        public static void Shutdown()
        {
            Execute("shutdown");
        }

        public override string Play()
        {
            SetPlaying(true);
            return Execute("commands/?cmd=play");
        }

        public override string Pause()
        {
            SetPlaying(false);
            return Execute("commands/?cmd=pause");
        }

        public override string Next()
        {
            return Execute("commands/?cmd=next");
        }

        public override string Prev()
        {
            return Execute("commands/?cmd=prev");
        }

        public override string ChangeTrackTo(string fileName)
        {
            return "";
        }

        public override bool RandomToggle(byte diskNumber)
        {
            return true;
        }
    }
}
