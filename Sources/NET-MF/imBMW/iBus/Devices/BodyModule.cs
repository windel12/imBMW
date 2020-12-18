using System.Threading;
using imBMW.Tools;
using imBMW.Enums;

namespace imBMW.iBus.Devices.Real
{
    #region Enums, delegales and event args

    public enum RemoteKeyButton 
    {
        Lock, 
        Unlock,
        Trunk
    }

    public enum BusType
    {
        IBus,
        KBus
    }

    public class RemoteKeyEventArgs
    {
        public RemoteKeyButton Button { get; private set; }
        public BusType Bus { get; private set; }

        public RemoteKeyEventArgs(RemoteKeyButton button, BusType bus)
        {
            Button = button;
            Bus = bus;
        }
    }

    public delegate void RemoteKeyButtonEventHandler(RemoteKeyEventArgs e);

    #endregion

    /// <summary>
    /// Body Module class. Aka General Module 5 (GM5) or ZKE5.
    /// </summary>
    public static class BodyModule
    {
        static Timer timer;
        #region Messages

        static Message MessageOpenTrunk = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open trunk", 0x0C, 0x95, 0x01);

        static Message MessageLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Lock doors", 0x0C, 0x4F, 0x01); // 0x0C, 0x97, 0x01
        static Message MessageLockDriverDoor = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Lock driver door", 0x0C, 0x47, 0x01);
        static Message MessageUnlockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unlock doors", 0x0C, 0x45, 0x01); // 0x0C, 0x03, 0x01
        static Message MessageToggleLockDoors = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Toggle lock doors", 0x0C, 0x03, 0x01); // TODO after it sometimes can't open usign hardware button
        static Message MessageRequestDoorsStatus = new Message(DeviceAddress.InstrumentClusterElectronics, DeviceAddress.BodyModule, "Request doors status", 0x79);

        //static Message MessageOpenWindows = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, 0x0C, 0x00, 0x65);
        
        public static Message MessageOpenWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver front window", 0x0C, 0x52, 0x01);
        public static Message MessageOpenWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open driver rear window", 0x0C, 0x41, 0x01);
        public static Message MessageOpenWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger front window", 0x0C, 0x54, 0x01);
        public static Message MessageOpenWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open passenger rear window", 0x0C, 0x44, 0x01);

        public static Message MessageCloseWindowDriverFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver front window", 0x0C, 0x53, 0x01);
        public static Message MessageCloseWindowDriverRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close driver rear window", 0x0C, 0x42, 0x01);
        public static Message MessageCloseWindowPassengerFront = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger front window", 0x0C, 0x55, 0x01);
        public static Message MessageCloseWindowPassengerRear = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close passenger rear window", 0x0C, 0x43, 0x01);

        static Message MessageOpenSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Open sunroof", 0x0C, 0x7E, 0x01);
        static Message MessageCloseSunroof = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Close sunroof", 0x0C, 0x7F, 0x01);

        static Message MessageFoldDriverMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Fold driver mirror", 0x0C, 0x01, 0x31, 0x01);
        static Message MessageFoldPassengerMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Fold passenger mirror", 0x0C, 0x02, 0x31, 0x01);
        static Message MessageUnfoldDriverMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unfold driver mirror", 0x0C, 0x01, 0x30, 0x01);
        static Message MessageUnfoldPassengerMirrorE39 = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Unfold passenger mirror", 0x0C, 0x02, 0x30, 0x01);

        static Message MessageFoldMirrorsE46 = new Message(DeviceAddress.MirrorMemorySecond, DeviceAddress.MirrorMemory, "Fold mirrors", 0x6D, 0x90);
        static Message MessageUnfoldMirrorsE46 = new Message(DeviceAddress.MirrorMemorySecond, DeviceAddress.MirrorMemory, "Unfold mirrors", 0x6D, 0xA0);

        static Message MessageGetAnalogValues = new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Get analog values", 0x0B, 0x10); // E39 fix; was - 0x0B, 0x01

        #endregion

        static double batteryVoltage;
        static bool isCarLocked;
        static bool wasDriverDoorOpened;

        static BodyModule()
        {
            KBusManager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.BodyModule, message => ProcessGMMessage(message, BusType.KBus));
            Manager.Instance.AddMessageReceiverForSourceDevice(DeviceAddress.BodyModule, message => ProcessGMMessage(message, BusType.IBus));
            InstrumentClusterElectronics.IgnitionStateChanged += InstrumentClusterElectronics_IgnitionStateChanged;
        }

        /// <summary>
        /// Does nothing. Just to call static constructor.
        /// </summary>
        public static void Init() { }

        static void ProcessGMMessage(Message m, BusType bus)
        {
            if (m.Data.Length == 2 && m.Data[0] == 0x72) // Remote Keys
            {
                var btn = m.Data[1];
                if (btn.HasBit(4)) // 0x1_
                {
                    OnRemoteKeyButton(m, RemoteKeyButton.Lock, bus);
                }
                else if (btn.HasBit(5)) // 0x2_
                {
                    OnRemoteKeyButton(m, RemoteKeyButton.Unlock, bus);
                }
                else if (btn.HasBit(6)) // 0x4_
                {
                    OnRemoteKeyButton(m, RemoteKeyButton.Trunk, bus);
                }
                else
                {
                    m.ReceiverDescription = "No button pressed";
                }
            }
            if (m.Data.Length == 3 && m.Data[0] == 0x7A) // Doors/windows status
            {
                // Data[1] = 7654 3210. 7 = ??, 6 = light, 5 = lock, 4 = unlock, 5+4 = hard lock,
                //      doors statuses: 0 = left front (driver), 1 = right front, 2 = left rear, 3 = right rear.
                // Car could have locked status even after doors are opened!
                // Data[2] = 7654 3210. 5 = trunk.
                isCarLocked = m.Data[1].HasBit(5);
                if (isCarLocked)
                {
                    if (m.Data[1].HasBit(0))
                    {
                        wasDriverDoorOpened = true;
                    }
                }
                else
                {
                    wasDriverDoorOpened = false;
                }

                if (m.Data[1].HasBit(0)) { m.ReceiverDescription += "FrontLeft door opened;";}
                if (m.Data[1].HasBit(1)) { m.ReceiverDescription += "FrontRight door opened;";}
                if (m.Data[1].HasBit(2)) { m.ReceiverDescription += "RearLeft door opened;";}
                if (m.Data[1].HasBit(3)) { m.ReceiverDescription += "RearRight door opened;";}
                if (m.Data[1].HasBit(4) && !m.Data[1].HasBit(5)) { m.ReceiverDescription += "Car unlocked;";}
                if (m.Data[1].HasBit(5)) { m.ReceiverDescription += "Car locked;";}
                if (m.Data[1].HasBit(6)) { m.ReceiverDescription += "Interior lights ON;";}

                if (m.Data[2].HasBit(0)) { m.ReceiverDescription += "FrontLeft window opened;";}
                if (m.Data[2].HasBit(1)) { m.ReceiverDescription += "FrontRight window opened;";}
                if (m.Data[2].HasBit(2)) { m.ReceiverDescription += "RearLeft window opened;";}
                if (m.Data[2].HasBit(3)) { m.ReceiverDescription += "RearRight window opened;";}
                if (m.Data[2].HasBit(4)) { m.ReceiverDescription += "Sunroof opened;";}
                if (m.Data[2].HasBit(5)) { m.ReceiverDescription += "TrunkLid opened;";}
                if (m.Data[2].HasBit(6)) { m.ReceiverDescription += "Hood opened;";}

                if (DoorStatusChanged != null)
                {
                    DoorStatusChanged(m.Data[1]);
                }
            }
            if (m.Data.Length > 3 && m.Data[0] == 0xA0)
            {
                // TODO filter not analog-values responses
                var voltage = ((double)m.Data[1]) / 10 + ((double)m.Data[2]) / 1000;

                m.ReceiverDescription = "Analog values. Battery voltage = " + voltage + "V";
                BatteryVoltage = voltage;
            }
        }

        static void OnRemoteKeyButton(Message m, RemoteKeyButton button, BusType bus)
        {
            var e = RemoteKeyButtonPressed;
            if (e != null)
            {
                e(new RemoteKeyEventArgs(button, bus));
            }
            m.ReceiverDescription = "Remote key: '" + button.ToStringValue() + "' was pressed";
        }

        private static void InstrumentClusterElectronics_IgnitionStateChanged(IgnitionEventArgs e)
        {
            if (e.CurrentIgnitionState != IgnitionState.Ign && e.PreviousIgnitionState == IgnitionState.Ign)
            {
                BatteryVoltage = 0;
            }
        }

        public static bool IsCarLocked
        {
            get { return isCarLocked; }
        }

        public static double BatteryVoltage
        {
            get { return batteryVoltage; }
            private set
            {
                // always notify to know that message was received
                /*if (batteryVoltage == value)
                {
                    return;
                }*/
                batteryVoltage = value;

                var e = BatteryVoltageChanged;
                if (e != null)
                {
                    e(value);
                }
            }
        }

        public static void UpdateBatteryVoltage()
        {
            KBusManager.Instance.EnqueueMessage(MessageGetAnalogValues);
        }

        public static void OpenTrunk()
        {
            KBusManager.Instance.EnqueueMessage(MessageOpenTrunk);
        }

        public static void LockDoors()
        {
            if (!isCarLocked || wasDriverDoorOpened)
            {
                isCarLocked = true;
                wasDriverDoorOpened = false;
                KBusManager.Instance.EnqueueMessage(MessageToggleLockDoors, MessageRequestDoorsStatus);
            }
        }

        public static bool UnlockDoors()
        {
            if (!isCarLocked || wasDriverDoorOpened)
            {
                return !isCarLocked;
            }
            isCarLocked = wasDriverDoorOpened;
            wasDriverDoorOpened = false;
            KBusManager.Instance.EnqueueMessage(MessageToggleLockDoors, MessageRequestDoorsStatus);
            return !isCarLocked;
        }

        /// <summary>
        /// Warning! Opens windows just by half!
        /// </summary>
        public static void OpenWindows()
        {
            KBusManager.Instance.EnqueueMessage(MessageOpenWindowDriverFront, 
                MessageOpenWindowPassengerFront, 
                MessageOpenWindowPassengerRear,
                MessageOpenWindowDriverRear);
        }

        /// <summary>
        /// Warning! Closes windows just by half!
        /// </summary>
        public static void CloseWindows()
        {
            KBusManager.Instance.EnqueueMessage(MessageCloseWindowDriverFront,
                MessageCloseWindowPassengerFront,
                MessageCloseWindowPassengerRear,
                MessageCloseWindowDriverRear);
        }

        public static void OpenSunroof()
        {
            KBusManager.Instance.EnqueueMessage(MessageOpenSunroof);
        }

        public static void CloseSunroof()
        {
            KBusManager.Instance.EnqueueMessage(MessageCloseSunroof);
        }

        /// <summary>
        /// Now only E39 mirrors are supported, E46 not tested
        /// </summary>
        public static void FoldMirrors()
        {
            KBusManager.Instance.EnqueueMessage(MessageFoldMirrorsE46,
                MessageFoldPassengerMirrorE39,
                MessageFoldDriverMirrorE39);
        }

        /// <summary>
        /// Now only E39 mirrors are supported, E46 not tested
        /// </summary>
        public static void UnfoldMirrors()
        {
            KBusManager.Instance.EnqueueMessage(MessageUnfoldMirrorsE46,
                MessageUnfoldPassengerMirrorE39,
                MessageUnfoldDriverMirrorE39);
        }

        public static void RequestDoorWindowStatus()
        {
            KBusManager.Instance.EnqueueMessage(new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.BodyModule, 0x79)); // Doors/windows status request
        }

        public static void RequestDoorWindowStatusViaIbus()
        {
            Manager.Instance.EnqueueMessage(new Message(DeviceAddress.OnBoardMonitor, DeviceAddress.BodyModule, 0x79)); // Doors/windows status request
        }

        public static void SleepMode(byte timeout)
        {
            Logger.Trace("Going to execute sleep mode via ZKE with timeout in " + timeout + " seconds.");
            if (InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Ign || InstrumentClusterElectronics.CurrentIgnitionState == IgnitionState.Acc)
            {
                Logger.Warning("Going to sleep mode.");
            }

            timer = new Timer(delegate
            {
                Logger.Warning("ZKE SLEEP");
                KBusManager.Instance.EnqueueMessage(new Message(DeviceAddress.Diagnostic, DeviceAddress.BodyModule, "Sleep mode", 0x9D));
            }, null, timeout * 1000, 0);
        }

        public static event RemoteKeyButtonEventHandler RemoteKeyButtonPressed;
        public static event ActionByte DoorStatusChanged;

        public static event ActionDouble BatteryVoltageChanged;
    }
}
