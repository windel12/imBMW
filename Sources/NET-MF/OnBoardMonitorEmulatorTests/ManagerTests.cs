using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using imBMW.iBus;
using imBMW.iBus.Devices.Real;
using imBMW.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class ManagerTests
    {
        [TestMethod]
        public void ShouldCreateDBusMessage()
        {
            var message = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE, 0x30, 0xC7, 0x07, 0x1E);
            var messageFromBytes = DBusMessage.TryCreate(message.Packet);

            CollectionAssert.AreEqual(message.Packet, new byte[] { 0xB8, 0x12, 0xF1, 0x04, 0x30, 0xC7, 0x07, 0x1E, 0xB1 });
            CollectionAssert.AreEqual(message.Packet, messageFromBytes.Packet);
            
        }

        [TestMethod]
        public void ShouldCreateDBusMessageWith10Params()
        {
            var message = new DBusMessage(DeviceAddress.OBD, DeviceAddress.DDE,
                new byte[] { 0x2C, 0x10 }
                .Combine(DigitalDieselElectronics.admVDF)
                .Combine(DigitalDieselElectronics.dzmNmit)
                .Combine(DigitalDieselElectronics.ldmP_Lsoll)
                .Combine(DigitalDieselElectronics.ldmP_Llin)
                .Combine(DigitalDieselElectronics.ehmFLDS)
                .Combine(DigitalDieselElectronics.zumPQsoll)
                .Combine(DigitalDieselElectronics.zumP_RAIL)
                .Combine(DigitalDieselElectronics.ehmFKDR)
                .Combine(DigitalDieselElectronics.mrmM_EAKT)
                .Combine(DigitalDieselElectronics.aroIST_4));

            CollectionAssert.AreEqual(message.Packet, new byte[] {
                0xB8, 0x12, 0xF1, 0x16,
                0x2C, 0x10,
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
        }

        [TestMethod]
        public void ShouldCorrectlyParseEluefterRequest()
        {
            bool messageReceived = false;
            DBusManager.Instance.AfterMessageReceived += e =>
            {
                messageReceived = true;
            };

            var buffer = new byte[] { 0xB8 };

            var mock = new Mock<ISerialPort>();
            mock.Setup(x => x.AvailableBytes).Returns(() => buffer.Length);
            mock.Setup(x => x.ReadAvailable()).Returns(() => buffer);

            DBusManager.Instance.bus_DataReceived(mock.Object, null);

            buffer = new byte[] { 0x12, 0xF1, 0x04, 0x30, 0xC7, 0x07, 0x1E, 0xB1 };
            DBusManager.Instance.bus_DataReceived(mock.Object, null);

            Assert.IsTrue(messageReceived);
        }

        [TestMethod]
        public void ShouldCorrectlyHandleNotFullIBusMessage()
        {
            var message = new Message(DeviceAddress.LightControlModule, DeviceAddress.GlobalBroadcastAddress, 0x5B, 0x09, 0x80, 0x00, 0x00);

            var notFullPacket = message.Packet.SkipAndTake(0, message.Packet.Length - 3);
            CollectionAssert.AreEqual(notFullPacket, new byte[] { 0xD0, 0x07, 0xBF, 0x5B, 0x09, 0x80, });

            var mock = new Mock<ISerialPort>();
            mock.Setup(x => x.AvailableBytes).Returns(() => notFullPacket.Length);
            mock.Setup(x => x.ReadAvailable()).Returns(() => notFullPacket);

            Manager.Instance.bus_DataReceived(mock.Object, null);

        }

        [TestMethod]
        public void ShouldCorrectlyHandleNotFullDBusMessage()
        {
            bool messageReceived = false;
            DBusManager.Instance.AfterMessageReceived += e =>
            {
                messageReceived = true;
            };

            var buffer = new byte[]
            {
                0xB8, 0x12, 0xF1, 0x16,
                0x2C, 0x10,

                0x20, 0x06, // admVDF
                0x0F, 0x10, // dzmNmit
                0x0F, 0x42, // ldmP_Lsoll
                0x0F, 0x40, // ldmP_Llin
                0x0E, 0x81, // ehmFLDS
                0x1F, 0x5E, // zumPQsoll
                0x1F, 0x5D, // zumP_RAIL
                0x0E, 0xE5, // ehmFKDR
                //0x0F, 0x80 //mrmM_EAKT
                //0x00, 0x10 //aroIST_4
            };

            var mock = new Mock<ISerialPort>();
            mock.Setup(x => x.AvailableBytes).Returns(() => buffer.Length);
            mock.Setup(x => x.ReadAvailable()).Returns(() => buffer);

            DBusManager.Instance.bus_DataReceived(mock.Object, null);

            Assert.IsFalse(messageReceived, "should not receive message, because bytes in buffer not contains full packet");
        }

        [TestMethod]
        public void ShouldCorrectlySkipNonDBusBytes()
        {
            bool messageReceived = false;
            DBusManager.Instance.AfterMessageReceived += e =>
            {
                messageReceived = true;
            };

            var buffer = new byte[0];
            var mock = new Mock<ISerialPort>();
            mock.Setup(x => x.AvailableBytes).Returns(() => buffer.Length);
            mock.Setup(x => x.ReadAvailable()).Returns(() => buffer);

            buffer = new byte[]
            {
                0x23, 0x40, 0x02, 0xFF, 0xFF, 0xFF, 0xFC, 0xC0 // some data, shich send DDE after start
            };
            DBusManager.Instance.bus_DataReceived(mock.Object, null);

            buffer = new byte[] 
            {
                0xB8, 0x12, 0xF1, 0x16,
                0x2C, 0x10,
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
                0xB2
            };
            DBusManager.Instance.bus_DataReceived(mock.Object, null);

            Assert.IsFalse(messageReceived, "should not receive message, because bytes in buffer not contains full packet");
        }
    }
}
