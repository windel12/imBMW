using System;
using System.IO;
using System.Linq;
using imBMW.Tools;
using Microsoft.SPOT.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OnBoardMonitorEmulatorTests
{
    [TestClass]
    public class SettingsTests
    {
        [TestMethod]
        public void Should_ReadSettingsFromFile_And_RewriteValueInFile_IfChanged()
        {
            string path = VolumeInfo.GetVolumes()[0].RootDirectory + "\\imBMW.ini";
            File.Delete(path);


            Settings.Init(path);
            Assert.AreEqual(Settings.Instance.UnmountMassStorageOnChangingIgnitionToAcc, false);
            Assert.AreEqual(Settings.Instance.ForceMessageLog, false);
            Assert.AreEqual(Settings.Instance.LightsBlinkerTimeout, 250);


            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.WriteLine(CreateSettings(nameof(Settings.Instance.UnmountMassStorageOnChangingIgnitionToAcc), "true"));
                sw.WriteLine(CreateSettings(nameof(Settings.Instance.LightsBlinkerTimeout), 200));
            }
            Settings.Init(path);
            var lines = File.ReadLines(path).ToArray();
            Assert.AreEqual(lines.Length, 2);
            Assert.AreEqual(lines.ElementAt(0), string.Format("{0}={1}", nameof(Settings.Instance.UnmountMassStorageOnChangingIgnitionToAcc), "true"));
            Assert.AreEqual(Settings.Instance.UnmountMassStorageOnChangingIgnitionToAcc, true);
            Assert.AreEqual(Settings.Instance.LightsBlinkerTimeout, 200);
            Assert.AreEqual(Settings.Instance.ForceMessageLog, false);


            Settings.Instance.ForceMessageLog = true;
            lines = File.ReadLines(path).ToArray();
            var line0 = string.Format("{0}={1}", nameof(Settings.Instance.UnmountMassStorageOnChangingIgnitionToAcc), "True");
            var line1 = string.Format("{0}={1}", nameof(Settings.Instance.ForceMessageLog), "True");
            Assert.AreEqual(lines.Length, 3);
            Assert.AreEqual(lines[0], line0);
            Assert.AreEqual(lines[1], line1);

            Settings.Init(path);
            Assert.AreEqual(Settings.Instance.UnmountMassStorageOnChangingIgnitionToAcc, true);
            Assert.AreEqual(Settings.Instance.ForceMessageLog, true);
        }

        private string CreateSettings(string name, int value)
        {
            return CreateSettings(name, value.ToString());
        }

        private string CreateSettings(string name, string value)
        {
            var line = string.Format("{0}={1}", name, value);
            return line;
        }
    }
}
