using System;
using Microsoft.SPOT;
using System.Threading;

namespace imBMW.Features.Menu.Screens
{
    public class DDEScreen : MenuScreen
    {
        protected static DDEScreen instance;

        protected Timer refreshTimer;
        private int refreshRate = 1000;
        private Random r = new Random();

        public DDEScreen()
        {
            FastMenuDrawing = true;

            ClearItems();
            AddItem(new MenuItem(i => "Increase refresh rate", x =>
            {
                refreshRate -= 200;
                UpdateRefreshTimer();
            }));
            AddItem(new MenuItem(i => "Decrease refresh rate", x =>
            {
                refreshRate += 200;
                UpdateRefreshTimer();
            }));
            AddItem(new MenuItem(i => "admIDV: " + r.Next(1000)));
            AddItem(new MenuItem(i => "admKDF: " + r.Next(1000)));
            AddItem(new MenuItem(i => "admLDF: " + r.Next(1000)));
            AddItem(new MenuItem(i => "admLMM: " + r.Next(1000)));
            AddItem(new MenuItem(i => "admLTF: " + r.Next(1000)));
            AddItem(new MenuItem(i => "admPWG: " + r.Next(1000)));
            AddItem(new MenuItem(i => "refresh rate: " + refreshRate.ToString()));

            this.AddBackButton();
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                refreshRate = 1000;
                refreshTimer = new Timer(delegate
                {
                    OnUpdated(MenuScreenUpdateReason.Refresh);
                }, null, 500, refreshRate);
                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                if (refreshTimer != null)
                {
                    refreshTimer.Dispose();
                    refreshTimer = null;
                }
                return true;
            }
            return false;
        }

        private void UpdateRefreshTimer()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Dispose();
                refreshTimer = null;
            }
            refreshTimer = new Timer(delegate
            {
                OnUpdated(MenuScreenUpdateReason.Refresh);
            }, null, 0, refreshRate);
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
