﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OnBoardMonitorEmulator.DevicesEmulation
{
    public static class VolumioEmulator
    {
        private static ViewModel _viewModel;

        public static bool IsReady
        {
            get { return _viewModel.VolumioReadiness; }
            set { _viewModel.VolumioReadiness = value; }
        }

        public static string MakeHttpRequest(string param)
        {
            Thread.Sleep(200); // simulate delay

            if (!IsReady)
            {
                Thread.Sleep(1000); // simulate timeout
                throw new WebException("isn't ready", WebExceptionStatus.ConnectFailure);
            }

            switch (param)
            {
                case "reboot":
                case "shutdown":
                    IsReady = false;
                    return "";
                case "commands/?cmd=pause":
                    return "{\"time\":1584619195432,\"response\":\"pause Success\"}";
                case "commands/?cmd=play":
                    return "{\"time\":1584619195432,\"response\":\"play Success\"}";
                case "commands/?cmd=stop":
                    return "{\"time\":1584619195432,\"response\":\"stop Success\"}";
                case "commands/?cmd=next":
                    return "\"Next track name" + new Random().Next(0, 255) + "\"";
                case "commands/?cmd=prev":
                    return "Prev track name" + new Random().Next(0, 255);
                case "ping":
                    return "pong";
            }

            return "Unknown param";
        }

        public static void Init(ViewModel viewModel)
        {
            _viewModel = viewModel;
        }
    }
}
