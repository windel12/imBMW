using System;
using System.Threading;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.Features.Localizations;
using imBMW.iBus.Devices.Emulators;
using imBMW.Multimedia;
using imBMW.Features.Multimedia.Models;
using Microsoft.SPOT.Hardware;

namespace imBMW.Features.Menu.Screens
{
    public class BordcomputerScreen : MenuScreen
    {
        protected static BordcomputerScreen instance;

        protected MenuItem itemPlayer;
        protected MenuItem itemFav;
        protected MenuItem itemBC;
        protected MenuItem itemSettings;

        protected DateTime lastUpdated;
        protected DateTime lastVoltageUpdated;
        protected bool needUpdateVoltage;

        protected byte updateLimitSeconds = 1;
        protected byte updateVoltageLimitSeconds = 5;

        private ushort refreshInterval = 1000;
        private ushort timeoutBeforeStart = 2000;
        protected Timer refreshTimer;

        // TODO: refactor
        public MediaEmulator MediaEmulator { get; set; }

        protected BordcomputerScreen()
        {
            FastMenuDrawing = true;
            SetItems();

            InstrumentClusterElectronics.RequestConsumption();
            InstrumentClusterElectronics.RequestAverageSpeed();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                NavigationModule.BatteryVoltageChanged += BodyModule_BatteryVoltageChanged;
                InstrumentClusterElectronics.SpeedRPMChanged += InstrumentClusterElectronics_SpeedRPMChanged;
                InstrumentClusterElectronics.TemperatureChanged += InstrumentClusterElectronics_TemperatureChanged;
                InstrumentClusterElectronics.AverageSpeedChanged += InstrumentClusterElectronics_AverageSpeedChanged;
                InstrumentClusterElectronics.Consumption1Changed += InstrumentClusterElectronics_Consumption1Changed;
                InstrumentClusterElectronics.Consumption2Changed += InstrumentClusterElectronics_Consumption2Changed;
                InstrumentClusterElectronics.RangeChanged += InstrumentClusterElectronics_RangeChanged;
                InstrumentClusterElectronics.SpeedLimitChanged += InstrumentClusterElectronics_SpeedLimitChanged;

                //IntegratedHeatingAndAirConditioning.AuxilaryHeaterWorkingRequestsCounterChanged += IntegratedHeatingAndAirConditioning_AuxilaryHeaterWorkingRequestsCounterChanged;

                //MediaEmulator.Player.TrackChanged += TrackChanged;
                //refreshTimer = new Timer(delegate
                //{
                //    OnUpdateHeader(MenuScreenUpdateReason.Refresh);
                //}, null, timeoutBeforeStart, refreshInterval);

                UpdateVoltage();
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                NavigationModule.BatteryVoltageChanged -= BodyModule_BatteryVoltageChanged;
                InstrumentClusterElectronics.SpeedRPMChanged -= InstrumentClusterElectronics_SpeedRPMChanged;
                InstrumentClusterElectronics.TemperatureChanged -= InstrumentClusterElectronics_TemperatureChanged;
                InstrumentClusterElectronics.AverageSpeedChanged -= InstrumentClusterElectronics_AverageSpeedChanged;
                InstrumentClusterElectronics.Consumption1Changed -= InstrumentClusterElectronics_Consumption1Changed;
                InstrumentClusterElectronics.Consumption2Changed -= InstrumentClusterElectronics_Consumption2Changed;
                InstrumentClusterElectronics.RangeChanged -= InstrumentClusterElectronics_RangeChanged;
                InstrumentClusterElectronics.SpeedLimitChanged -= InstrumentClusterElectronics_SpeedLimitChanged;

                //IntegratedHeatingAndAirConditioning.AuxilaryHeaterWorkingRequestsCounterChanged -= IntegratedHeatingAndAirConditioning_AuxilaryHeaterWorkingRequestsCounterChanged;

                //MediaEmulator.Player.TrackChanged -= TrackChanged;
                //if (refreshTimer != null)
                //{
                //    refreshTimer.Dispose();
                //    refreshTimer = null;
                //}

                return true;
            }
            return false;
        }

        //private void TrackChanged(IAudioPlayer sender, TrackInfo nowPlaying)
        //{
        //    refreshTimer.Change(timeoutBeforeStart, refreshInterval);
        //}

        //private void IntegratedHeatingAndAirConditioning_AuxilaryHeaterWorkingRequestsCounterChanged(byte counter)
        //{
        //    OnUpdateHeader(MenuScreenUpdateReason.Refresh);
        //}

        private void InstrumentClusterElectronics_SpeedLimitChanged(SpeedLimitEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_RangeChanged(RangeEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_Consumption2Changed(ConsumptionEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_Consumption1Changed(ConsumptionEventArgs e)
        {
            UpdateItems();
        }

        private void InstrumentClusterElectronics_AverageSpeedChanged(AverageSpeedEventArgs e)
        {
            UpdateItems();
        }

        void InstrumentClusterElectronics_TemperatureChanged(TemperatureEventArgs e)
        {
            UpdateItems();
        }

        void InstrumentClusterElectronics_SpeedRPMChanged(SpeedRPMEventArgs e)
        {
            UpdateItems();
        }

        void BodyModule_BatteryVoltageChanged(double voltage)
        {
            if (voltage == 0)
            {
                needUpdateVoltage = true;
            }
            UpdateItems(voltage == 0);
        }

        protected bool UpdateItems(bool force = false)
        {
            var now = DateTime.Now;
            int span;
            if ((now - lastVoltageUpdated).GetTotalSeconds() > updateVoltageLimitSeconds || lastUpdated == DateTime.MinValue) //(needUpdateVoltage) // span > updateLimitSeconds / 2 && 
            {
                UpdateVoltage();
                lastVoltageUpdated = now;
            }
            if (!force && lastUpdated != DateTime.MinValue && (span = (now - lastUpdated).GetTotalSeconds()) < updateLimitSeconds)
            {
                return false;
            }
            lastUpdated = now;
            OnUpdateBody(MenuScreenUpdateReason.Refresh);
            //OnUpdateHeader(MenuScreenUpdateReason.RefreshWithDelay, (ushort)1000);
            needUpdateVoltage = true;
            return true;
        }

        protected virtual uint FirstColumnLength
        {
            get
            {
                var l = System.Math.Max(Localization.Current.Speed.Length, Localization.Current.Revs.Length);
                l = System.Math.Max(l, Localization.Current.Voltage.Length);
                l = System.Math.Max(l, Localization.Current.Engine.Length);
                l = System.Math.Max(l, Localization.Current.Outside.Length);
                return (uint)(l + 3);
            }
        }


        protected virtual void SetItems()
        {
            ClearItems();

            AddItem(new MenuItem(i => Localization.Current.Speed + ": " + InstrumentClusterElectronics.CurrentSpeed + Localization.Current.KMH));
            AddItem(new MenuItem(i => Localization.Current.Revs + ": " + InstrumentClusterElectronics.CurrentRPM));
            AddItem(new MenuItem(i => Localization.Current.Consumption + ": " 
                + (InstrumentClusterElectronics.Consumption1 == 0 ? "-" : InstrumentClusterElectronics.Consumption1.ToString("F1")) 
                + "/"
                + (InstrumentClusterElectronics.Consumption2 == 0 ? "-" : InstrumentClusterElectronics.Consumption2.ToString("F1"))
            /*, i => InstrumentClusterElectronics.ResetConsumption1()*/)
            );
            //AddItem(new MenuItem(i => Localization.Current.Consumption + " 2: " + (InstrumentClusterElectronics.Consumption2 == 0 ? "-" : InstrumentClusterElectronics.Consumption2.ToString("F1"))
            ///*, i => InstrumentClusterElectronics.ResetConsumption2()*/)
            //);
            AddItem(new MenuItem(i => Localization.Current.Average + ": " + (InstrumentClusterElectronics.AverageSpeed == 0 ? "-" : InstrumentClusterElectronics.AverageSpeed.ToString("F1") + Localization.Current.KMH)));
            AddItem(new MenuItem(i => Localization.Current.Range + ": " + (InstrumentClusterElectronics.Range == 0 ? "-" : InstrumentClusterElectronics.Range.ToString())));


            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " + (NavigationModule.BatteryVoltage > 0 ? NavigationModule.BatteryVoltage.ToString("F2") : "-") + " " + Localization.Current.VoltageShort, i => UpdateVoltage()));
            AddItem(new MenuItem(i =>
            {
                var coolant = InstrumentClusterElectronics.TemperatureCoolant == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureCoolant.ToString();
                return Localization.Current.Engine + ": " + coolant + Localization.Current.DegreeCelsius;
            }));
            AddItem(new MenuItem(i =>
            {
                var outside = InstrumentClusterElectronics.TemperatureOutside == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureOutside.ToString();
                return Localization.Current.Outside + ": " + outside + Localization.Current.DegreeCelsius;
            }));
            //AddItem(new MenuItem(i => Localization.Current.Limit + ": " + (InstrumentClusterElectronics.SpeedLimit == 0 ? "-" : InstrumentClusterElectronics.SpeedLimit + Localization.Current.KMH), MenuItemType.Button, MenuItemAction.GoToScreen)
            //{
            //    GoToScreenCallback = () => { return SpeedLimitScreen.Instance; }
            //});
            AddItem(new MenuItem(i => 
                Localization.Current.Compressor + ": " 
                + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_FirstByte.ToString("X")                              
                + " "               
                + IntegratedHeatingAndAirConditioning.AirConditioningCompressorStatus_SecondByte.ToString("X")));
            this.AddBackButton();
        }

        protected void UpdateVoltage()
        {
            needUpdateVoltage = false;
            NavigationModule.UpdateBatteryVoltage();
        }

        public static BordcomputerScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BordcomputerScreen();
                }
                return instance;
            }
        }

        //private Random r = new Random();
        //public override string T1Field
        //{
        //    //get { return MediaEmulator != null ? (MediaEmulator.Player.CurrentTrack.Time.ToString()) : ""; }
        //    get { return "T1" + r.Next(99); }
        //}

        //public override string T2Field
        //{
        //    get { return "T2" + r.Next(9); }
        //}

        public override string T3Field
        {
            get { return MediaEmulator != null ? (MediaEmulator.Player.IsRandom ? "RND" : "") : ""; }
            //get { return "T3" + r.Next(999); }
        }

        //public override string T4Field
        //{
        //    get { return "T4" + r.Next(9); }
        //}

        //public override string T5Field
        //{
        //    get { return "T5" + r.Next(999); }
        //}
    }
}
