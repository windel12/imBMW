using System;
using System.IO;

namespace imBMW.Tools
{
    public class Settings
    {
        public static short _step1 = 50;
        public static short _step2 = 100;
        public static short _step3 = 200;
        public static short _step4 = 1000;

        static Settings instance;

        protected string settingsPath;
        
        public bool Log { get; set; }

        public bool LogToSD { get; set; }

        public bool LogMessageToASCII { get; set; } = true;

        public bool AutoLockDoors { get; set; }

        public bool AutoUnlockDoors { get; set; }

        public bool AutoCloseWindows { get; set; }

        public bool AutoCloseSunroof { get; set; }

        public bool MenuMFLControl { get; set; } = true;

        public bool RadioSpaceCharAlt { get; set; }

        public NaviVersion NaviVersion { get; set; } = NaviVersion.MK4;

        public MenuMode MenuMode { get; set; } = MenuMode.BordmonitorCDC;

        public string Language { get; set; }

        public string BluetoothPin { get; set; } = "0000";

        public string MediaShield { get; set; }

        public int LightsBlinkerTimeout { get; set; } = 250;

        private bool _unmountMassStorageOnChangingIgnitionToAcc = false;
        public bool UnmountMassStorageOnChangingIgnitionToAcc
        {
            get { return _unmountMassStorageOnChangingIgnitionToAcc; }
            set
            {
                if (_unmountMassStorageOnChangingIgnitionToAcc != value)
                {
                    _unmountMassStorageOnChangingIgnitionToAcc = value;
                    SettingsChanged();
                }
            }
        }

        private bool _forceMessageLog = false;
        public bool ForceMessageLog
        {
            get { return _forceMessageLog; }
            set
            {
                if (_forceMessageLog != value)
                {
                    _forceMessageLog = value;
                    SettingsChanged();
                }
            }
        }

        private bool _suspendCDChangerResponseEmulation = false;
        public bool SuspendCDChangerResponseEmulation
        {
            get { return _suspendCDChangerResponseEmulation; }
            set
            {
                if (_suspendCDChangerResponseEmulation != value)
                {
                    _suspendCDChangerResponseEmulation = value;
                    SettingsChanged();
                }
            }
        }

        private bool _suspendAuxilaryHeaterResponseEmulation = true;
        public bool SuspendAuxilaryHeaterResponseEmulation
        {
            get { return _suspendAuxilaryHeaterResponseEmulation; }
            set
            {
                if (_suspendAuxilaryHeaterResponseEmulation != value)
                {
                    _suspendAuxilaryHeaterResponseEmulation = value;
                    SettingsChanged();
                }
            }
        }

        private bool _watchdogResetOnIKEResponse = false;
        public bool WatchdogResetOnIKEResponse
        {
            get { return _watchdogResetOnIKEResponse; }
            set
            {
                if (_watchdogResetOnIKEResponse != value)
                {
                    _watchdogResetOnIKEResponse = value;
                    SettingsChanged();
                }
            }
        }

        private bool _autoRestartVolumio = false;
        public bool AutoRestartVolumio
        {
            get { return _autoRestartVolumio; }
            set
            {
                if (_autoRestartVolumio != value)
                {
                    _autoRestartVolumio = value;
                    SettingsChanged();
                }
            }
        }

        private short _delay1 = _step1;
        public short Delay1
        {
            get { return _delay1; }
            set
            {
                if (_delay1 != value)
                {
                    _delay1 = value;
                    SettingsChanged();
                }
            }
        }

        private short _delay2 = _step2;
        public short Delay2
        {
            get { return _delay2; }
            set
            {
                if (_delay2 != value)
                {
                    _delay2 = value;
                    SettingsChanged();
                }
            }
        }

        private short _delay3 = _step3;
        public short Delay3
        {
            get { return _delay3; }
            set
            {
                if (_delay3 != value)
                {
                    _delay3 = value;
                    SettingsChanged();
                }
            }
        }

        private short _delay4 = _step4;
        public short Delay4
        {
            get { return _delay4; }
            set
            {
                if (_delay4 != value)
                {
                    _delay4 = value;
                    SettingsChanged();
                }
            }
        }

        public static Settings Init(string path)
        {
            Instance = new Settings();
            if (path != null)
            {
                if (File.Exists(path))
                {
                    Instance.InitFile(path);
                }
                else
                {
                    Instance.settingsPath = path;
                }
            }
            else
            {
                Logger.Warning("No settings file");
            }
            return Instance;
        }


        protected virtual void InitFile(string path)
        {
            try
            {
                using (var sr = new StreamReader(path))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s == string.Empty || s[0] == '#')
                        {
                            continue;
                        }
                        var parts = s.Split('=');
                        ProcessSetting(parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : null);
                    }
                }
                settingsPath = path;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error in reading settings from file");
            }
        }

        private void SettingsChanged()
        {
            if (settingsPath == null)
                return;

            try
            {
                using (var sw = new StreamWriter(settingsPath))
                {
                    sw.WriteLine(nameof(UnmountMassStorageOnChangingIgnitionToAcc) + "=" + UnmountMassStorageOnChangingIgnitionToAcc);
                    sw.WriteLine(nameof(ForceMessageLog) + "=" + ForceMessageLog);
                    sw.WriteLine(nameof(SuspendCDChangerResponseEmulation) + "=" + SuspendCDChangerResponseEmulation);
                    sw.WriteLine(nameof(SuspendAuxilaryHeaterResponseEmulation) + "=" + SuspendAuxilaryHeaterResponseEmulation);
                    sw.WriteLine(nameof(WatchdogResetOnIKEResponse) + "=" + WatchdogResetOnIKEResponse);
                    sw.WriteLine(nameof(AutoRestartVolumio) + "=" + AutoRestartVolumio);
                    sw.WriteLine(nameof(Delay1) + "=" + Delay1);
                    sw.WriteLine(nameof(Delay2) + "=" + Delay2);
                    sw.WriteLine(nameof(Delay3) + "=" + Delay3);
                    sw.WriteLine(nameof(Delay4) + "=" + Delay4);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "error in writing settings file");
            }
        }

        protected virtual void ProcessSetting(string name, string value)
        {
            try
            {
                Logger.Debug("Setting: " + name + " = " + (value ?? ""));
                value = value.ToLower();
                bool isTrue = value == "1" || value == "true" || value == "on" || value == "yes";
                switch (name)
                {
                    case nameof(AutoLockDoors):
                        AutoLockDoors = isTrue;
                        break;
                    case nameof(AutoUnlockDoors):
                        AutoUnlockDoors = isTrue;
                        break;
                    case nameof(AutoCloseWindows):
                        AutoCloseWindows = isTrue;
                        break;
                    case nameof(AutoCloseSunroof):
                        AutoCloseSunroof = isTrue;
                        break;
                    case nameof(Log):
                        Log = isTrue;
                        break;
                    case nameof(LogToSD):
                        LogToSD = isTrue;
                        break;
                    case nameof(LogMessageToASCII):
                        LogMessageToASCII = isTrue;
                        break;
                    case nameof(NaviVersion.MK2): // Deprecated. Use NaviVersion
                        NaviVersion = Tools.NaviVersion.MK2;
                        break;
                    case nameof(MenuMFLControl):
                        MenuMFLControl = isTrue;
                        break;
                    case nameof(RadioSpaceCharAlt):
                        RadioSpaceCharAlt = isTrue;
                        break;
                    case nameof(MenuMode):
                        MenuMode = (Tools.MenuMode)byte.Parse(value);
                        break;
                    case nameof(NaviVersion):
                        NaviVersion = (Tools.NaviVersion)byte.Parse(value);
                        break;
                    case nameof(Language):
                        Language = value;
                        break;
                    case nameof(BluetoothPin):
                        BluetoothPin = value;
                        break;
                    case nameof(MediaShield):
                        MediaShield = value; // TODO make enum
                        break;
                    case nameof(LightsBlinkerTimeout):
                        LightsBlinkerTimeout = int.Parse(value);
                        break;
                    case nameof(UnmountMassStorageOnChangingIgnitionToAcc):
                        _unmountMassStorageOnChangingIgnitionToAcc = isTrue;
                        break;
                    case nameof(ForceMessageLog):
                        _forceMessageLog = isTrue;
                        break;
                    case nameof(SuspendCDChangerResponseEmulation):
                        _suspendCDChangerResponseEmulation = isTrue;
                        break;
                    case nameof(SuspendAuxilaryHeaterResponseEmulation):
                        _suspendAuxilaryHeaterResponseEmulation = isTrue;
                        break;
                    case nameof(WatchdogResetOnIKEResponse):
                        _watchdogResetOnIKEResponse = isTrue;
                        break;
                    case nameof(AutoRestartVolumio):
                        _autoRestartVolumio = isTrue;
                        break;
                    case nameof(Delay1):
                        _delay1 = short.Parse(value);
                        break;
                    case nameof(Delay2):
                        _delay2 = short.Parse(value);
                        break;
                    case nameof(Delay3):
                        _delay3 = short.Parse(value);
                        break;
                    case nameof(Delay4):
                        _delay4 = short.Parse(value);
                        break;
                    default:
                        Logger.Warning("Unknown setting");
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "reading setting");
            }
        }

        public static Settings Instance
        {
            protected set
            {
                instance = value;
            }
            get
            {
                if (instance == null)
                {
                    Init(null);
                }
                return instance;
            }
        }
    }
}
