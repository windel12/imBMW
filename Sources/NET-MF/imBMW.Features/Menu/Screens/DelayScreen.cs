using imBMW.Tools;
using imBMW.iBus.Devices.Real;

namespace imBMW.Features.Menu.Screens
{
    public class DelayScreen : MenuScreen
    {
        protected static DelayScreen instance;

        private short _step = 50;

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
                Settings.Instance.Delay1 += _step;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay1--: " + Settings.Instance.Delay1, x =>
            {
                if (Settings.Instance.Delay1 > 0)
                    Settings.Instance.Delay1 -= _step;
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Delay2++: " + Settings.Instance.Delay2, x =>
            {
                Settings.Instance.Delay2 += _step;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay2--: " + Settings.Instance.Delay2, x =>
            {
                if (Settings.Instance.Delay2 > 0)
                    Settings.Instance.Delay2 -= _step;
            }, MenuItemType.Button, MenuItemAction.Refresh));

            this.AddDummyButton();


            AddItem(new MenuItem(i => "Delay3++: " + Settings.Instance.Delay3, x =>
            {
                Settings.Instance.Delay3 += _step;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay3--: " + Settings.Instance.Delay3, x =>
            {
                if (Settings.Instance.Delay3 > 0)
                    Settings.Instance.Delay3 -= _step;
            }, MenuItemType.Button, MenuItemAction.Refresh));

            AddItem(new MenuItem(i => "Delay4++: " + Settings.Instance.Delay4, x =>
            {
                Settings.Instance.Delay4 += _step;
            }, MenuItemType.Button, MenuItemAction.Refresh));
            AddItem(new MenuItem(i => "Delay4--: " + Settings.Instance.Delay4, x =>
            {
                if (Settings.Instance.Delay4 > 0)
                    Settings.Instance.Delay4 -= _step;
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
