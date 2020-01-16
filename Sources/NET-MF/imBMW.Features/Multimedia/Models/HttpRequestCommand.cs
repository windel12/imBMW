using System;

namespace imBMW.Features.Multimedia.Models
{
    internal class HttpRequestCommand
    {
        public string Param { get; }
        public ActionString Callback { get; }

        public HttpRequestCommand(string param)
        {
            Param = param;
        }

        public HttpRequestCommand(string param, ActionString callback) : this(param)
        {
            Callback = callback;
        }

#if OnBoardMonitorEmulator
            ~HttpRequestCommand()
            {
            }
#endif
    }
}
