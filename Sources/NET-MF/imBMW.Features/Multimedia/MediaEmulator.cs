using System;
using imBMW.iBus.Devices.Real;
using imBMW.Multimedia;

namespace imBMW.iBus.Devices.Emulators
{
    public delegate void MediaEmulatorEnabledEventHandler(MediaEmulator emulator, bool isEnabled);

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
            protected set
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
                player = value;
                player.IsPlayingChanged += player_IsPlayingChanged;
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

        public event IsPlayingHandler PlayerIsPlayingChanged;

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

        public virtual void Play()
        {
            if(Player.Inited)
                Player.Play();
        }

        public virtual void Pause()
        {
            if (Player.Inited)
                Player.Pause();
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
        }

        protected virtual void VoiceButtonLongPress()
        {
        }

        protected virtual void RandomToggle(byte diskNumber)
        {
            if (Player.Inited)
                Player.RandomToggle(diskNumber);
            // TODO send rnd status to radio
        }

    }
}
