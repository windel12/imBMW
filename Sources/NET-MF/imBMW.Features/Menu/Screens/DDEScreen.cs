using System;
using System.Threading;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;

namespace imBMW.Features.Menu.Screens
{
    public class DDEScreen : MenuScreen
    {
        protected static DDEScreen instance;

        protected Timer refreshTimer;
        private int refreshRate = 1000;
        //private Random r = new Random();

        protected MenuItem item1;
        protected MenuItem item2;
        protected MenuItem item3;
        protected MenuItem item4;
        protected MenuItem item5;
        protected MenuItem item6;
        protected MenuItem item7;
        protected MenuItem item8;
        protected MenuItem item9;
        protected MenuItem item10;

        private static DBusMessage getDataMessage = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE,
                new byte[] { 0x2C, 0x10 }
                .Combine(DigitalDieselElectronics.admVDF)       // 0x20, 0x06,  
                .Combine(DigitalDieselElectronics.dzmNmit)      // 0x0F, 0x10,  
                .Combine(DigitalDieselElectronics.ldmP_Lsoll)   // 0x0F, 0x42,  
                .Combine(DigitalDieselElectronics.ldmP_Llin)    // 0x0F, 0x40,  
                .Combine(DigitalDieselElectronics.ehmFLDS)      // 0x0E, 0x81,  
                .Combine(DigitalDieselElectronics.zumPQsoll)    // 0x1F, 0x5E,  
                .Combine(DigitalDieselElectronics.zumP_RAIL)    // 0x1F, 0x5D,  
                .Combine(DigitalDieselElectronics.ehmFKDR)      // 0x0E, 0xE5,  
                .Combine(DigitalDieselElectronics.mrmM_EAKT)    // 0x0F, 0x80, 
                .Combine(DigitalDieselElectronics.aroIST_4));   // 0x00, 0x10, 

        private MenuItemEventHandler ItemClick = e =>
        {
            DBusManager.Instance.EnqueueMessage(getDataMessage);
        };

        public DDEScreen()
        {
            FastMenuDrawing = true;

            item1 = new MenuItem(x => "VDF: " + DigitalDieselElectronics.PresupplyPressure.ToString("F2"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item2 = new MenuItem(x => "RPM: " + DigitalDieselElectronics.Rpm.ToString("F0"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item3 = new MenuItem(x => "BoostTrg: " + DigitalDieselElectronics.BoostTarget.ToString("F0"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item4 = new MenuItem(x => "BoostAct: " + DigitalDieselElectronics.BoostActual.ToString("F0"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item5 = new MenuItem(x => "VNT: " + DigitalDieselElectronics.VNT.ToString("F0"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item6 = new MenuItem(x => "RailTrg: " + DigitalDieselElectronics.RailPressureTarget.ToString("F0"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item7 = new MenuItem(x => "RailAct: " + DigitalDieselElectronics.RailPressureActual.ToString("F0"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item8 = new MenuItem(x => "DRV: " + DigitalDieselElectronics.PressureRegulationValve.ToString("F0"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item9 = new MenuItem(x => "IQ: " + DigitalDieselElectronics.InjectionQuantity.ToString("F2"), ItemClick) { ShouldRefreshScreenIfTextChanged = false };
            item10 = new MenuItem(x => "LMM: " + DigitalDieselElectronics.AirMass.ToString("F2"), MenuItemType.Button, MenuItemAction.GoBackScreen);

            //item9 = new MenuItem(x => "armM_List: " + DigitalDieselElectronics.AirMassPerStroke) { ShouldRefreshScreenIfTextChanged = false };
            //item9 = new MenuItem("Refresh", (e) =>
            //{
            //    DBusManager.Port.WriteBufferSize = 1;
            //    DBusManager.Instance.EnqueueMessage(DigitalDieselElectronics.QueryMessage);
            //}, MenuItemType.Button, MenuItemAction.Refresh);


            //ClearItems();
            //AddItem(new MenuItem(i => "Increase refresh rate", x =>
            //{
            //    refreshRate -= 200;
            //    UpdateRefreshTimer();
            //}));
            //AddItem(new MenuItem(i => "Decrease refresh rate", x =>
            //{
            //    refreshRate += 200;
            //    UpdateRefreshTimer();
            //}));
            //AddItem(new MenuItem(i => "admIDV: " + r.Next(1000)));
            //AddItem(new MenuItem(i => "admKDF: " + r.Next(1000)));
            //AddItem(new MenuItem(i => "admLDF: " + r.Next(1000)));
            //AddItem(new MenuItem(i => "admLMM: " + r.Next(1000)));
            //AddItem(new MenuItem(i => "admLTF: " + r.Next(1000)));
            //AddItem(new MenuItem(i => "admPWG: " + r.Next(1000)));
            //AddItem(new MenuItem(i => "refresh rate: " + refreshRate.ToString()));

            AddItem(item1);
            AddItem(item2);
            AddItem(item3);
            AddItem(item4);
            AddItem(item5);
            AddItem(item6);
            AddItem(item7);
            AddItem(item8);
            AddItem(item9);
            AddItem(item10);
            //this.AddBackButton();

            Logger.Debug("protected DDEScreen()");
        }

        private void DigitalDieselElectronics_MessageReceived()
        {
            Refresh();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                DigitalDieselElectronics.MessageReceived += DigitalDieselElectronics_MessageReceived;
                //refreshRate = 1000;
                //refreshTimer = new Timer(delegate
                //{
                //    DBusManager.Instance.EnqueueMessage(DigitalDieselElectronics.QueryMessage);
                //}, null, 500, refreshRate);
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                DigitalDieselElectronics.MessageReceived -= DigitalDieselElectronics_MessageReceived;
                //if (refreshTimer != null)
                //{
                //    refreshTimer.Dispose();
                //    refreshTimer = null;
                //}
                return true;
            }
            return false;
        }

        private void UpdateRefreshTimer()
        {
            refreshTimer.Change(0, refreshRate);
        }

        public static DDEScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DDEScreen();
                }
                return instance;
            }
        }
    }
}
