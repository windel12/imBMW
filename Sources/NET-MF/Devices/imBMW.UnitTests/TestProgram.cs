using MFUnit;
using imBMW.Features.Menu;

namespace imBMW.UnitTests
{
    public class TestProgram : TestApplication
    {
        public static void Main()
        {
            new TestProgram().Run();
        }
    }

    public class BordmonitorTests
    {
        public void ShouldTrimText_WithLength_LessThanTrimLength()
        {
            int startIndex;
            string testString = "Dua Lipa";
            int trimLength = 10;
            string result = "";

            Assert.IsTrue(testString.Length < trimLength);

            startIndex = 0;
            result = BordmonitorMenu.TrimTextToLength("Dua Lipa", ref startIndex, trimLength);
            Assert.AreEqual(startIndex, 0);
            Assert.AreEqual(result, "Dua Lipa");
        }

        public void ShouldTrimText_WithLength_EqualToTrimLength()
        {
            int startIndex;
            string testString = "Dua Lipa";
            int trimLength = testString.Length;
            string result = "";

            Assert.IsTrue(testString.Length == trimLength);

            startIndex = 0;
            result = BordmonitorMenu.TrimTextToLength("Dua Lipa", ref startIndex, trimLength);
            Assert.AreEqual(startIndex, 0);
            Assert.AreEqual(result, "Dua Lipa");
        }

        public void ShouldTrimText_WithLength_BiggerThanTrimLength()
        {
            int startIndex;
            string testString = "The Last Of The Real Ones";
            int trimLength = 10;
            string result = "";

            Assert.IsTrue(testString.Length > trimLength);

            startIndex = 0;
            result = BordmonitorMenu.TrimTextToLength(testString, ref startIndex, trimLength);
            Assert.AreEqual(startIndex, 1);
            Assert.AreEqual(result, "The Last O");

            startIndex = 5;
            result = BordmonitorMenu.TrimTextToLength(testString, ref startIndex, trimLength);
            Assert.AreEqual(startIndex, 6);
            Assert.AreEqual(result, "ast Of The");

            startIndex = 15;
            result = BordmonitorMenu.TrimTextToLength(testString, ref startIndex, trimLength);
            Assert.AreEqual(startIndex, 16);
            Assert.AreEqual(result, " Real Ones");

            startIndex = 16;
            result = BordmonitorMenu.TrimTextToLength(testString, ref startIndex, trimLength);
            Assert.AreEqual(startIndex, 1);
            Assert.AreEqual(result, "The Last O");

            startIndex = 1;
            result = BordmonitorMenu.TrimTextToLength(testString, ref startIndex, trimLength);
            Assert.AreEqual(startIndex, 2);
            Assert.AreEqual(result, "he Last Of");
        }
    }
}
