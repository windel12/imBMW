using System;

namespace imBMW.Features.Multimedia.Models
{
    internal class HttpRequestCommand
    {
        public string Param { get; }
        public ActionString SuccessCallback { get; }
        public ActionException ErrorCallback { get; }

        public HttpRequestCommand(string param, ActionString successCallback = null, ActionException errorCallback = null)
        {
            Param = param;
            SuccessCallback = successCallback;
            ErrorCallback = errorCallback;
        }

#if OnBoardMonitorEmulator
            ~HttpRequestCommand()
            {
            }
#endif
    }
}
