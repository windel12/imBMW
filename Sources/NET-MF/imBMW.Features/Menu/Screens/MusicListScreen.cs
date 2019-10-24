using System;
using System.Collections;
using System.IO;
using imBMW.Features.Localizations;
using imBMW.Features.Multimedia.Models;
using imBMW.iBus.Devices.Emulators;
using imBMW.Tools;
using Microsoft.SPOT.IO;

namespace imBMW.Features.Menu.Screens
{
    public class TrackMenuItem : MenuItem
    {
        public string FilePath;

        public TrackMenuItem(string text, MenuItemEventHandler callback) : base(text, callback)
        {
            ShouldRefreshScreenIfTextChanged = false;
        }
    }

    public class MusicListScreen : MenuScreen
    {
        protected static MusicListScreen instance;

        // TODO: refactor
        public delegate MediaEmulator GetMediaEmulatorHandler();
        public static GetMediaEmulatorHandler GetMediaEmulator;

        protected MenuItem item1;
        protected MenuItem item2;
        protected MenuItem item3;
        protected MenuItem item4;
        protected MenuItem item5;
        protected MenuItem item6;
        protected MenuItem item7;
        protected MenuItem nextPage;
        protected MenuItem prevPage;

        private byte pageNumber = 0;
        private byte itemsCount = 7;
        private bool lastItemReached;

        private IEnumerator filesEnumerator;

        protected MusicListScreen()
        {
            FastMenuDrawing = true;

            item1 = new TrackMenuItem("", ItemSelected);
            item2 = new TrackMenuItem("", ItemSelected);
            item3 = new TrackMenuItem("", ItemSelected);
            item4 = new TrackMenuItem("", ItemSelected);
            item5 = new TrackMenuItem("", ItemSelected);
            item6 = new TrackMenuItem("", ItemSelected);
            item7 = new TrackMenuItem("", ItemSelected);
            nextPage = new TrackMenuItem(Localization.Current.NextItems, NextPage);
            prevPage = new TrackMenuItem(Localization.Current.PrevItems, PrevPage);

            SetItems();
        }

        protected virtual void SetItems()
        {
            ClearItems();

            AddItem(item1);
            AddItem(item2);
            AddItem(item3);
            AddItem(item4);
            AddItem(item5);
            AddItem(item6);
            AddItem(item7);
            AddItem(nextPage);
            AddItem(prevPage);
            this.AddBackButton();
        }

        public void ItemSelected(MenuItem item)
        {
            var trackMenuItem = (TrackMenuItem)item;
            if (!StringHelpers.IsNullOrEmpty(trackMenuItem.FilePath))
            {
                GetMediaEmulator().Player.ChangeTrackTo(trackMenuItem.FilePath);
            }
        }

        public void NextPage(MenuItem item)
        {
            if (!lastItemReached)
            {
                ++pageNumber;
                GeneratePage();
                Refresh();
            }
        }

        public void PrevPage(MenuItem item)
        {
            if (pageNumber > 0)
            {
                lastItemReached = false;
                --pageNumber;

                InitFilesEnumerator();

                GeneratePage();
                Refresh();
            }
        }

        public void GeneratePage()
        {
            for (int i = 0; i < itemsCount; i++)
            {
                var trackMenuItem = (TrackMenuItem)Items[i];

                if (lastItemReached || !filesEnumerator.MoveNext())
                {
                    trackMenuItem.Text = "_-_";
                    trackMenuItem.FilePath = null;
                    lastItemReached = true;
                }
                else
                { 
                    TrackInfo trackInfo = new TrackInfo((string) filesEnumerator.Current);
                    trackMenuItem.Text = trackInfo.Title;
                    trackMenuItem.FilePath = trackInfo.FilePath;
                }
            }
        }

        public override bool OnNavigatedTo(MenuBase menu)
        {
            if (base.OnNavigatedTo(menu))
            {
                if (VolumeInfo.GetVolumes().Length > 0 && VolumeInfo.GetVolumes()[0].IsFormatted)
                {
                    InitFilesEnumerator();

                    GeneratePage();
                }

                return true;
            }
            return false;
        }

        public override bool OnNavigatedFrom(MenuBase menu)
        {
            if (base.OnNavigatedFrom(menu))
            {
                filesEnumerator = null;

                return true;
            }
            return false;
        }

        public void InitFilesEnumerator()
        {
            lastItemReached = false;

            string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
            var folder = rootDirectory + "\\" + GetMediaEmulator().Player.DiskNumber;
            filesEnumerator = Directory.EnumerateFiles(folder).GetEnumerator();

            GoToCurrentPage();
        }

        private void GoToCurrentPage()
        {
            for (int i = 0; i < itemsCount * pageNumber; i++)
                filesEnumerator.MoveNext(); // do nothing, just skip current file
        }

        public static MusicListScreen Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MusicListScreen();
                }
                return instance;
            }

        }
    }
}
    
