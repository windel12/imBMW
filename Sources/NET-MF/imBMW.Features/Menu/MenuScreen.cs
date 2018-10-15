using System;
using Microsoft.SPOT;
using System.Collections;
using imBMW.Tools;

namespace imBMW.Features.Menu
{
    public enum MenuScreenUpdateReason
    {
        Navigation,
        StatusChanged,
        ItemChanged,
        Refresh,
        Scroll
    }

    public class MenuScreenUpdateEventArgs : EventArgs
    {
        public object Item { get; protected set; }

        public MenuScreenUpdateReason Reason { get; set; }

        public MenuScreenUpdateEventArgs(MenuScreenUpdateReason reason, object item = null)
        {
            Reason = reason;
            Item = item;
        }
    }

    public delegate void MenuScreenEventHandler(MenuScreen screen);

    public delegate void MenuScreenUpdateEventHandler(MenuScreen screen, MenuScreenUpdateEventArgs args);

    public delegate void MenuScreenItemEventHandler(MenuScreen screen, MenuItem item);

    public delegate string MenuScreenGetTextHandler(MenuScreen screen);

    public class MenuScreen
    {
        string title;
        string status;
        bool updateSuspended;
        MenuBase parentMenu;

        public bool FastMenuDrawing { get; set; }

        public MenuScreen(string title = null)
            : this()
        {
            if (title != null)
            {
                Title = title;
            }
        }

        public MenuScreen(MenuScreenGetTextHandler titleCallback = null)
            : this()
        {
            TitleCallback = titleCallback;
        }

        public MenuScreen()
        {
            Items = new ArrayList();
        }

        static MenuScreen()
        {
            MaxItemsCount = 10;
        }

        public static int MaxItemsCount { get; set; } // TODO refactor

        public MenuScreenGetTextHandler TitleCallback { get; set; }
        public MenuScreenGetTextHandler StatusCallback { get; set; }

        public string Title
        {
            get
            {
                if (TitleCallback != null)
                {
                    title = TitleCallback(this);
                }
                return title;
            }
            set
            {
                if (title == value)
                {
                    return;
                }
                title = value;
                OnUpdateHeader(MenuScreenUpdateReason.Refresh);
            }
        }

        public string Status
        {
            get
            {
                if (StatusCallback != null)
                {
                    status = StatusCallback(this);
                }
                return status;
            }
            set
            {
                if (status == value)
                {
                    return;
                }
                status = value;
                OnUpdateHeader(MenuScreenUpdateReason.StatusChanged);
            }
        }

        public virtual string T1Field
        {
            get;
        }

        public virtual string T3Field
        {
            get;
        }

        public virtual string T5Field
        {
            get;
        }

        protected ArrayList Items { get; private set; }

        public int ItemsCount
        {
            get
            {
                return Items.Count;
            }
        }

        public MenuItem GetItem(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                return (MenuItem)Items[index];
            }
            return null;
        }

        public void AddItem(MenuItem menuItem, int index = -1)
        {
            if (index >= MaxItemsCount || index < 0 && ItemsCount == MaxItemsCount)
            {
                Logger.Error("Can't add screen item \"" + menuItem + "\" with index=" + index + ", count=" + ItemsCount);
                index = MaxItemsCount - 1;
            }
            if (index < 0)
            {
                Items.Add(menuItem);
            }
            else
            {
                if (index < Items.Count)
                {
                    UnsubscribeItem(Items[index] as MenuItem);
                    Items.RemoveAt(index);
                }
                while (index > Items.Count)
                {
                    Items.Add(null);
                }
                Items.Insert(index, menuItem);
            }
            menuItem.Changed += menuItem_Changed;
            menuItem.Clicked += menuItem_Clicked;
            OnUpdateBody(MenuScreenUpdateReason.Refresh);
        }

        public void ClearItems()
        {
            if (Items.Count > 0)
            {
                foreach (var i in Items)
                {
                    UnsubscribeItem(i as MenuItem);
                }
            }
            Items.Clear();
            OnUpdateHeader(MenuScreenUpdateReason.Refresh);
        }

        public virtual bool OnNavigatedTo(MenuBase menu)
        {
            if (parentMenu == menu)
            {
                return false;
            }
            if (parentMenu != null)
            {
                throw new Exception("Already navigated to screen " + this + " in another menu " + parentMenu + ". Can't navigate in " + menu);
            }
            parentMenu = menu;

            var e = NavigatedTo;
            if (e != null)
            {
                e(this);
            }

            return true;
        }

        public virtual bool OnNavigatedFrom(MenuBase menu)
        {
            if (parentMenu == menu)
            {
                parentMenu = null;

                var e = NavigatedFrom;
                if (e != null)
                {
                    e(this);
                }

                return true;
            }
            if (parentMenu != null)
            {
                throw new Exception("Navigated to screen " + this + " in another menu " + parentMenu + ". Can't navigate from in " + menu);
            }
            return false;
        }

        public override string ToString()
        {
            return Title;
        }

        public void WithUpdateSuspended(MenuScreenEventHandler callback)
        {
            IsUpdateSuspended = true;
            callback(this);
            IsUpdateSuspended = false;
        }

        public void Refresh()
        {
            OnUpdateBody(MenuScreenUpdateReason.Refresh);
        }

        /// <summary>
        /// Menu navigated to this screen and screen is not suspended (screen, but not screen update).
        /// </summary>
        public bool IsNavigated
        {
            get
            {
                return parentMenu != null;
            }
        }

        /// <summary>
        /// Is screen update suspended, eg. for batch update.
        /// </summary>
        public bool IsUpdateSuspended
        {
            get
            {
                return updateSuspended;
            }
            set
            {
                if (updateSuspended == value)
                {
                    return;
                }
                updateSuspended = value;
            }
        }

        public event MenuScreenItemEventHandler ItemClicked;

        public event MenuScreenUpdateEventHandler UpdateHeader;

        public event MenuScreenUpdateEventHandler UpdateBody;

        public event MenuScreenEventHandler NavigatedTo;

        public event MenuScreenEventHandler NavigatedFrom;

        protected void UnsubscribeItem(MenuItem item)
        {
            if (item == null)
            {
                return;
            }
            item.Clicked -= menuItem_Clicked;
            item.Changed -= menuItem_Changed;
        }

        protected void menuItem_Clicked(MenuItem item)
        {
            OnItemClicked(item);
        }

        protected void menuItem_Changed(MenuItem item)
        {
            OnUpdateBody(MenuScreenUpdateReason.ItemChanged, item);
        }

        protected void OnUpdateHeader(MenuScreenUpdateReason reason, object item = null)
        {
            if (updateSuspended)
            {
                return;
            }
            var e = UpdateHeader;
            if (e != null)
            {
                e(this, new MenuScreenUpdateEventArgs(reason, item));
            }
        }

        protected void OnUpdateBody(MenuScreenUpdateReason reason, object item = null)
        {
            if (updateSuspended)
            {
                return;
            }
            var e = UpdateBody;
            if (e != null)
            {
                e(this, new MenuScreenUpdateEventArgs(reason, item));
            }
        }

        protected void OnItemClicked(MenuItem item)
        {
            var e = ItemClicked;
            if (e != null)
            {
                e(this, item);
            }
        }
    }
}
