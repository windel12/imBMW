using System;
using System.Threading;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using imBMW.Features.Localizations;
using imBMW.Features.Multimedia;

namespace imBMW.Features.Menu.Screens
{
    public class BordcomputerScreen : MenuScreen
    {
        protected static BordcomputerScreen instance;

        //protected MenuItem itemPlayer;
        //protected MenuItem itemFav;
        //protected MenuItem itemBC;
        //protected MenuItem itemSettings;

        protected DateTime lastUpdated;
        protected DateTime lastDiagDataUpdated;
        protected bool needUpdateVoltage;

        protected byte updateLimitSeconds = 1;
        protected byte updateDiagDataLimitSeconds = 5;

        private ushort refreshInterval = 1000;
        private ushort timeoutBeforeStart = 2000;
        //protected Timer refreshTimer;

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
                InstrumentClusterElectronics.SpeedRPMChanged += InstrumentClusterElectronics_SpeedRPMChanged;
                InstrumentClusterElectronics.TemperatureChanged += InstrumentClusterElectronics_TemperatureChanged;
                InstrumentClusterElectronics.AverageSpeedChanged += InstrumentClusterElectronics_AverageSpeedChanged;
                InstrumentClusterElectronics.Consumption1Changed += InstrumentClusterElectronics_Consumption1Changed;
                InstrumentClusterElectronics.Consumption2Changed += InstrumentClusterElectronics_Consumption2Changed;
                InstrumentClusterElectronics.RangeChanged += InstrumentClusterElectronics_RangeChanged;
                InstrumentClusterElectronics.SpeedLimitChanged += InstrumentClusterElectronics_SpeedLimitChanged;

                NavigationModule.BatteryVoltageChanged += BodyModule_BatteryVoltageChanged;
                LightControlModule.HeatingTimeChanged += LightControlModule_ThermalOilLevelSensorTimingsChanged;
                LightControlModule.CoolingTimeChanged += LightControlModule_ThermalOilLevelSensorTimingsChanged;
                //IntegratedHeatingAndAirConditioning.AuxilaryHeaterWorkingRequestsCounterChanged += IntegratedHeatingAndAirConditioning_AuxilaryHeaterWorkingRequestsCounterChanged;

                //MediaEmulator.Player.TrackChanged += TrackChanged;
                //refreshTimer = new Timer(delegate
                //{
                //    OnUpdateHeader(MenuScreenUpdateReason.Refresh);
                //}, null, timeoutBeforeStart, refreshInterval);

                RequestSomeDiagData();
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                InstrumentClusterElectronics.SpeedRPMChanged -= InstrumentClusterElectronics_SpeedRPMChanged;
                InstrumentClusterElectronics.TemperatureChanged -= InstrumentClusterElectronics_TemperatureChanged;
                InstrumentClusterElectronics.AverageSpeedChanged -= InstrumentClusterElectronics_AverageSpeedChanged;
                InstrumentClusterElectronics.Consumption1Changed -= InstrumentClusterElectronics_Consumption1Changed;
                InstrumentClusterElectronics.Consumption2Changed -= InstrumentClusterElectronics_Consumption2Changed;
                InstrumentClusterElectronics.RangeChanged -= InstrumentClusterElectronics_RangeChanged;
                InstrumentClusterElectronics.SpeedLimitChanged -= InstrumentClusterElectronics_SpeedLimitChanged;

                NavigationModule.BatteryVoltageChanged -= BodyModule_BatteryVoltageChanged;
                LightControlModule.HeatingTimeChanged -= LightControlModule_ThermalOilLevelSensorTimingsChanged;
                LightControlModule.CoolingTimeChanged -= LightControlModule_ThermalOilLevelSensorTimingsChanged;
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

        void LightControlModule_ThermalOilLevelSensorTimingsChanged(double voltage)
        {
            UpdateItems();
        }


        protected bool UpdateItems(bool force = false)
        {
            var now = DateTime.Now;
            int span;
            if ((now - lastDiagDataUpdated).GetTotalSeconds() > updateDiagDataLimitSeconds || lastUpdated == DateTime.MinValue) //(needUpdateVoltage) // span > updateLimitSeconds / 2 && 
            {
                RequestSomeDiagData();
                lastDiagDataUpdated = now;
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

            AddItem(new MenuItem(i => Localization.Current.Voltage + ": " 
                + (NavigationModule.BatteryVoltage > 0 ? NavigationModule.BatteryVoltage.ToString("F2") : "-") 
                + " " 
                + Localization.Current.VoltageShort, 
                i => RequestSomeDiagData()));

            AddItem(new MenuItem(i =>
                {
                    if (Settings.Instance.OilLevelSensorDisplayTemp)
                    {
                        string oilTemp = "";
                        if (LightControlModule.HeatingTime > 0)
                        {
                            var heatingTime = LightControlModule.HeatingTime * 2000;
                            oilTemp = heatingTime.ToString("F0");
                        }
                        else
                        {
                            oilTemp = "-";
                        }

                        return Localization.Current.Oil + ": " + oilTemp + Localization.Current.DegreeCelsius;
                    }
                    else
                    {
                        return Localization.Current.Oil + ": "
                                                        + (LightControlModule.HeatingTime > 0
                                                            ? LightControlModule.HeatingTime.ToString("F4")
                                                            : "-")
                                                        + "/"
                                                        + (LightControlModule.CoolingTime > 0
                                                            ? LightControlModule.CoolingTime.ToString("F4")
                                                            : "-");
                    }
                }, 
                i =>
                {
                    LightControlModule.UpdateThermalOilLevelSensorValues();
                    Settings.Instance.OilLevelSensorDisplayTemp = !Settings.Instance.OilLevelSensorDisplayTemp;
                    Refresh();
                }));

            AddItem(new MenuItem(i =>
            {
                var coolant = InstrumentClusterElectronics.TemperatureCoolant == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureCoolant.ToString();
                var outside = InstrumentClusterElectronics.TemperatureOutside == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureOutside.ToString();
                return Localization.Current.Temp + ": " + coolant + Localization.Current.DegreeCelsius + "/" + outside + Localization.Current.DegreeCelsius;
            }));
            //AddItem(new MenuItem(i =>
            //{
            //    var outside = InstrumentClusterElectronics.TemperatureOutside == sbyte.MinValue ? "-" : InstrumentClusterElectronics.TemperatureOutside.ToString();
            //    return Localization.Current.Outside + ": " + outside + Localization.Current.DegreeCelsius;
            //}));
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

        protected void RequestSomeDiagData()
        {
            needUpdateVoltage = false;
            NavigationModule.UpdateBatteryVoltage();
            //LightControlModule.UpdateThermalOilLevelSensorValues();
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
