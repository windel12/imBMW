using System;
using imBMW.Tools;
using GHI.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using imBMW.iBus;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class SignalGeneratorTests
    {
        [TestMethod]
        public void ShouldEmulateUart8E1_9600()
        {
            var signalGenerator = new SignalGenerator(FEZPandaIII.Gpio.D29, true);
            uint delay = 3000;
            var message = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10, 0x0F, 0x00);
            // TODO: cover case, when 'initialValue' in Set method is true
            var buffer = signalGenerator.Set(false, message.Packet, delay, true);
            CollectionAssert.AreEqual(message.Packet, new byte[] { 0xB8, 0x12, 0xF1, 0x04, 0x2C, 0x10, 0x0F, 0x00, 0x6C });
            CollectionAssert.AreEqual(buffer, new uint[] {
                104 + 312, 312, 104, 104, 104, delay,   // B8
                208, 104, 208, 104, 416, delay,         // 12
                104, 104, 312, 520 + delay,             // F1
                312, 104, 520, 104 + delay,             // 04
                312, 208, 104, 104, 208, 104 + delay,   // 2C
                520, 104, 312, 104 + delay,             // 10
                104, 416, 520, delay,                   // 0F
                1040, delay,                            // 00
                312, 208, 104, 208, 208                 // 6C
            });
        }

        [TestMethod]
        public void ShouldEmulateUart8E1_9600_10params()
        {
            var signalGenerator = new SignalGenerator(FEZPandaIII.Gpio.D29, true);
            uint delay = 3000;
            var message = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x2C, 0x10,
                0x20, 0x06,     // admVDF
                0x0F, 0x10,     // dzmNmit
                0x0F, 0x42,     // ldmP_Lso
                0x0F, 0x40,     // ldmP_Lli
                0x0E, 0x81,     // ehmFLDS
                0x1F, 0x5E,     // zumPQsol
                0x1F, 0x5D,     // zumP_RAI
                0x0E, 0xE5,     // ehmFKDR
                0x0F, 0x80,     // mrmM_EAK
                0x00, 0x10);    // aroIST_4);
            // TODO: cover case, when 'initialValue' in Set method is true
            var buffer = signalGenerator.Set(false, message.Packet, delay, true);
            CollectionAssert.AreEqual(message.Packet, new byte[] { 0xB8, 0x12, 0xF1, 0x16, 0x2C, 0x10,
                0x20, 0x06,
                0x0F, 0x10,
                0x0F, 0x42,
                0x0F, 0x40,
                0x0E, 0x81,
                0x1F, 0x5E,
                0x1F, 0x5D,
                0x0E, 0xE5,
                0x0F, 0x80,
                0x00, 0x10,
                0xB2 });

            CollectionAssert.AreEqual(buffer, new uint[] {
                104 + 312, 312, 104, 104, 104, delay,           // B8
                208, 104, 208, 104, 416, delay,                 // 12
                104, 104, 312, 520 + delay,                     // F1
                104 + 104, 208, 104, 104, 312, 104 + delay,     // 16
                312, 208, 104, 104, 208, 104 + delay,           // 2C
                520, 104, 312, 104 + delay,                     // 10
                104, 416, 520, delay,                           // 0F
                1040, delay,                                    // 00
                //312, 208, 104, 208, 208                         // 6C
            });
        }
    }
}
