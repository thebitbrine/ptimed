using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ptimed
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();

            if (args.Any() && args[0].ToLower() == "-s")
                p.Run(0);
            else
                while (true)
                    try { if (args.Any()) p.Run(int.Parse(args[0])); else p.Run(); }
                    catch { }
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetSystemTime(ref SYSTEMTIME st);
        public void Run(int UpdateFreq = 10000)
        {
            TimeSpan TickDiff;
            var LocalTime = DateTime.Now.ToUniversalTime();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var RemoteTime = GetNetworkTime().ToUniversalTime();
            TickDiff = new TimeSpan(-(sw.ElapsedTicks) + (RemoteTime.Ticks - LocalTime.Ticks));
            sw.Stop();
            SYSTEMTIME st = new SYSTEMTIME();
            st.wYear = (short)RemoteTime.Year;
            st.wMonth = (short)RemoteTime.Month;
            st.wDay = (short)RemoteTime.Day;
            st.wHour = (short)RemoteTime.Hour;
            st.wMinute = (short)RemoteTime.Minute;
            st.wSecond = (short)RemoteTime.Second;
            st.wMilliseconds = (short)RemoteTime.Millisecond;
            SetSystemTime(ref st);
            Console.WriteLine($"[{RemoteTime.ToLocalTime().ToShortDateString()} {RemoteTime.ToLocalTime().ToString("HH:m:ss:ffff")}] Updated! Time difference: {TickDiff.TotalMilliseconds.ToString().Replace("-","")} ms");
            System.Threading.Thread.Sleep(UpdateFreq);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }


        public static DateTime GetNetworkTime()
        {
            //default Windows time server
            const string ntpServer = "time.windows.com";

            // NTP message size - 16 bytes of the digest (RFC 2030)
            var ntpData = new byte[48];

            //Setting the Leap Indicator, Version Number and Mode values
            ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            //The UDP port number assigned to NTP is 123
            var ipEndPoint = new IPEndPoint(addresses[0], 123);
            //NTP uses UDP

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(ipEndPoint);

                //Stops code hang if NTP is blocked
                socket.ReceiveTimeout = 3000;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

            //Offset to get to the "Transmit Timestamp" field (time at which the reply 
            //departed the server for the client, in 64-bit timestamp format."
            const byte serverReplyTime = 40;

            //Get the seconds part
            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            //Get the seconds fraction
            ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            //Convert From big-endian to little-endian
            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

            //**UTC** time
            var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        // stackoverflow.com/a/3294698/162671
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }

        public string InsertText(string Line, string Text, int Position)
        {
            int Index = 0;
            char[] LineArr = Line.ToCharArray();

            return string.Join("", LineArr);
        }

        #region Essentials
        public string LogPath = @"data\Logs.txt";
        public bool NoConsolePrint = false;
        public bool NoFilePrint = false;
        public void Print(string String)
        {
            Check();
            if (!NoFilePrint) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "")));
            if (!NoConsolePrint) Console.Write(Tag(String));
        }
        public void Print(string String, bool DoTag)
        {
            Check();
            if (DoTag) { if (!NoFilePrint) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", ""))); if (!NoConsolePrint) Console.Write(Tag(String)); }
            else { if (!NoFilePrint) WaitWrite(Rooter(LogPath), String.Replace("\r", "")); if (!NoConsolePrint) Console.Write(String); }
        }
        public void PrintLine(string String)
        {
            Check();
            if (!NoFilePrint) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + Environment.NewLine));
            if (!NoConsolePrint) Console.WriteLine(Tag(String));
        }
        public void PrintLine(string String, bool DoTag)
        {
            Check();
            if (DoTag) { if (!NoFilePrint) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + Environment.NewLine)); if (!NoConsolePrint) Console.WriteLine(Tag(String)); }
            else { if (!NoFilePrint) WaitWrite(Rooter(LogPath), String.Replace("\r", "") + Environment.NewLine); if (!NoConsolePrint) Console.WriteLine(String); }
        }
        public void PrintLine()
        {
            Check();
            if (!NoFilePrint) WaitWrite(Rooter(LogPath), Environment.NewLine);
            if (!NoConsolePrint) Console.WriteLine();
        }
        public void PrintLines(string[] StringArray)
        {
            Check();
            foreach (string String in StringArray)
            {
                if (!NoFilePrint) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + Environment.NewLine));
                if (!NoConsolePrint) Console.WriteLine(Tag(String));
            }
        }
        public void PrintLines(string[] StringArray, bool DoTag)
        {
            Check();
            foreach (string String in StringArray)
            {
                if (DoTag) { if (!NoFilePrint) WaitWrite(Rooter(LogPath), Tag(String.Replace("\r", "") + Environment.NewLine)); if (!NoConsolePrint) Console.WriteLine(Tag(String)); }
                else { if (!NoFilePrint) WaitWrite(Rooter(LogPath), String.Replace("\r", "") + Environment.NewLine); if (!NoConsolePrint) Console.WriteLine(String); }
            }
        }
        public void Check()
        {
            if (!NoFilePrint && !System.IO.File.Exists(LogPath)) Touch(LogPath);
        }
        private bool WriteLock = false;
        public void WaitWrite(string Path, string Data)
        {
            while (WriteLock) { System.Threading.Thread.Sleep(20); }
            WriteLock = true;
            System.IO.File.AppendAllText(Path, Data);
            WriteLock = false;
        }
        public string[] ReadData(string DataDir)
        {
            if (System.IO.File.Exists(DataDir))
            {
                List<string> Data = System.IO.File.ReadAllLines(DataDir).ToList<string>();
                foreach (var Line in Data)
                {
                    if (Line == "\n" || Line == "\r" || Line == "\t" || string.IsNullOrWhiteSpace(Line))
                        Data.Remove(Line);
                }
                return Data.ToArray();
            }
            else
                return null;
        }
        public string ReadText(string TextDir)
        {
            if (System.IO.File.Exists(TextDir))
            {
                return System.IO.File.ReadAllText(TextDir);
            }
            return null;
        }
        public string SafeJoin(string[] Array)
        {
            if (Array != null && Array.Length != 0)
                return string.Join("\r\n", Array);
            else return "";
        }
        public void CleanLine()
        {
            Console.Write("\r");
            for (int i = 0; i < Console.WindowWidth - 1; i++) Console.Write(" ");
            Console.Write("\r");
        }
        public void CleanLastLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            CleanLine();
        }
        public string Rooter(string RelPath)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), RelPath);
        }
        public static string StaticRooter(string RelPath)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), RelPath);
        }
        public string Tag(string Text)
        {
            return "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "] " + Text;
        }
        public string Tag()
        {
            return "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "] ";
        }
        public bool Touch(string Path)
        {
            try
            {
                System.Text.StringBuilder PathCheck = new System.Text.StringBuilder();
                string[] Direcories = Path.Split(System.IO.Path.DirectorySeparatorChar);
                foreach (var Directory in Direcories)
                {
                    PathCheck.Append(Directory);
                    string InnerPath = PathCheck.ToString();
                    if (System.IO.Path.HasExtension(InnerPath) == false)
                    {
                        PathCheck.Append("\\");
                        if (System.IO.Directory.Exists(InnerPath) == false) System.IO.Directory.CreateDirectory(InnerPath);
                    }
                    else
                    {
                        System.IO.File.WriteAllText(InnerPath, "");
                    }
                }
                if (IsDirectory(Path) && System.IO.Directory.Exists(PathCheck.ToString())) { return true; }
                if (!IsDirectory(Path) && System.IO.File.Exists(PathCheck.ToString())) { return true; }
            }
            catch (Exception ex) { PrintLine("ERROR: Failed touching \"" + Path + "\". " + ex.Message, true); }
            return false;
        }
        public bool IsDirectory(string Path)
        {
            try
            {
                System.IO.FileAttributes attr = System.IO.File.GetAttributes(Path);
                if ((attr & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                    return true;
                else
                    return false;
            }
            catch
            {
                if (System.IO.Path.HasExtension(Path)) return true;
                else return false;
            }
        }
        #endregion
    }
}
