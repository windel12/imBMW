//using System.IO;
//using System.Text;

//namespace imBMW.Tools
//{
//    public static class StreamHelpers
//    {
//        public static void WriteString(this FileStream stream, string str)
//        {
//            byte[] data = Encoding.UTF8.GetBytes(str);
//            stream.Write(data, 0, data.Length);
//        }

//        public static void WriteLine(this FileStream stream, string str)
//        {
//            byte[] data = Encoding.UTF8.GetBytes(str + "\n");
//            stream.Write(data, 0, data.Length);
//        }
//    }
//}
