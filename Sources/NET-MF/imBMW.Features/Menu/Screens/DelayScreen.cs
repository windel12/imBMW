using imBMW.iBus;
using imBMW.Tools;
using imBMW.iBus.Devices.Real;

namespace imBMW.Features.Menu.Screens
{
    public class DelayScreen : MenuScreen
    {
        protected static DelayScreen instance;

        protected DelayScreen()
        {
            FastMenuDrawing = true;

            TitleCallback = s => "Delays";
            //StatusCallback = s => AuxilaryHeater.Status.ToStringValue();

            AuxilaryHeater.Init();
        }

        protected virtual void SetItems()
        {
            AddItem(new MenuItem(i => "Delay1++: " + Settings.Instance.Delay1, x =>
            {
                Settings.Instance.Delay1 += Settings._step1;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay1--: " + Settings.Instance.Delay1, x =>
            {
                short value = (short) (Settings.Instance.Delay1 - Settings._step1);
                Settings.Instance.Delay1 = (short)(value >= 0 ? value : 0);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Delay2++: " + Settings.Instance.Delay2, x =>
            {
                Settings.Instance.Delay2 += Settings._step2;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay2--: " + Settings.Instance.Delay2, x =>
            {
                short value = (short)(Settings.Instance.Delay2 - Settings._step2);
                Settings.Instance.Delay2 = (short) (value >= 0 ? value : 0);
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "ZKE TrunkLid opened", x =>
                {
                    Manager.Instance.EnqueueMessage(new Message(DeviceAddress.BodyModule, DeviceAddress.GlobalBroadcastAddress, 0x7A, 0x10, 0x20));
                }, MenuItemType.Button, MenuItemAction.Refresh));


            AddItem(new MenuItem(i => "Delay3++: " + Settings.Instance.Delay3, x =>
            {
                Settings.Instance.Delay3 += Settings._step3;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay3--: " + Settings.Instance.Delay3, x =>
            {
                short value = (short)(Settings.Instance.Delay3 - Settings._step3);
                Settings.Instance.Delay3 = (short) (value >= 0 ? value : 0);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Delay4++: " + Settings.Instance.Delay4, x =>
            {
                Settings.Instance.Delay4 += Settings._step4;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay4--: " + Settings.Instance.Delay4, x =>
            {
                short value = (short)(Settings.Instance.Delay4 - Settings._step4);
                Settings.Instance.Delay4 = (short) (value >= 0 ? value : 0);
            }, MenuItemType.Button, MenuItemAction.Refresh));

            this.AddBackButton();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                SetItems();
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                ClearItems();
                return true;
            }
            return false;
        }

        public static DelayScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DelayScreen();
                }
                return instance;
            }
        }
    }
}
