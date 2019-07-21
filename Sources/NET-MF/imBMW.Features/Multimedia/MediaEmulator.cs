using System;
using Microsoft.SPOT;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;

namespace imBMW.iBus.Devices.Emulators
{
    public delegate void MediaEmulatorEnabledEventHandler(MediaEmulator emulator, bool isEnabled);

    public delegate void PlayerChangedHandler(IAudioPlayer sender);

    public abstract class MediaEmulator
    {
        private bool isEnabled;
        private IAudioPlayer player;

        protected bool mflModeTelephone;

        public MediaEmulator(IAudioPlayer player)
        {
            Player = player;

            MultiFunctionSteeringWheel.ButtonPressed += MultiFunctionSteeringWheel_ButtonPressed;
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            /*protected*/ set
            {
                if (isEnabled == value)
                {
                    return;
                }
                isEnabled = value;
                OnIsEnabledChanged(value);
            }
        }

        public event MediaEmulatorEnabledEventHandler IsEnabledChanged;

        protected virtual void OnIsEnabledChanged(bool isEnabled, bool fire = true)
        {
            if (fire)
            {
                FireIsEnabledChanged(isEnabled);
            }
        }

        protected void FireIsEnabledChanged(bool isEnabled)
        {
            var e = IsEnabledChanged;
            if (e != null)
            {
                e(this, isEnabled);
            }
        }

        protected void FireIsEnabledChanged()
        {
            FireIsEnabledChanged(IsEnabled);
        }

        public IAudioPlayer Player
        {
            get
            {
                return player;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (player != null)
                {
                    UnsetPlayer(player);
                }
                player = value;
                SetupPlayer(value);
            }
        }

        public event IsPlayingHandler PlayerIsPlayingChanged;

        public event PlayerStatusHandler PlayerStatusChanged;

        public event PlayerChangedHandler PlayerChanged;

        void SetupPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = true;
            player.IsPlayingChanged += player_IsPlayingChanged;
            player.StatusChanged += player_StatusChanged;

            var e = PlayerChanged;
            if (e != null)
            {
                e(player);
            }
        }

        void UnsetPlayer(IAudioPlayer player)
        {
            player.IsCurrentPlayer = false;
            player.IsPlayingChanged -= player_IsPlayingChanged;
            player.StatusChanged -= player_StatusChanged;
        }

        void player_StatusChanged(IAudioPlayer sender, string status, PlayerEvent playerEvent)
        {
            var e = PlayerStatusChanged;
            if (e != null)
            {
                e(sender, status, playerEvent);
            }
        }

        void player_IsPlayingChanged(IAudioPlayer sender, bool isPlaying)
        {
            var e = PlayerIsPlayingChanged;
            if (e != null)
            {
                e(sender, isPlaying);
            }
        }

        protected virtual void MultiFunctionSteeringWheel_ButtonPressed(MFLButton button)
        {
            switch (button)
            {
                case MFLButton.ModeRadio:
                    mflModeTelephone = false;
                    return;
                case MFLButton.ModeTelephone:
                    mflModeTelephone = true;
                    return;
            }
            if (IsEnabled && !mflModeTelephone)
            {
                switch (button)
                {
                    case MFLButton.Dial:
                        VoiceButtonPress();
                        break;
                    case MFLButton.DialLong:
                        VoiceButtonLongPress();
                        break;
                }
            }
        }

        protected virtual void Play()
        {
            if(Player.Inited)
                Player.Play();
        }

        protected virtual void Pause()
        {
            if (Player.Inited)
                Player.Pause();
        }

        protected virtual void PlayPauseToggle()
        {
            if (Player.Inited)
                Player.PlayPauseToggle();
        }

        protected virtual void Next()
        {
            if (Player.Inited)
                Player.Next();
        }

        protected virtual void Prev()
        {
            if (Player.Inited)
                Player.Prev();
        }

        protected virtual void VoiceButtonPress()
        {
            if (Player.Inited)
                Player.VoiceButtonPress();
        }

        protected virtual void VoiceButtonLongPress()
        {
            if (Player.Inited)
                Player.VoiceButtonLongPress();
        }

        protected virtual void RandomToggle(byte diskNumber)
        {
            if (Player.Inited)
                Player.RandomToggle(diskNumber);
            // TODO send rnd status to radio
        }

    }
}
