using System;
using System.IO.Ports;

namespace ParkerC3SerialComms
{
    static class Globals
    {
        public static SerialPort[] commhandle = new SerialPort[] { null, null };
        public static string[] comport = new string[] { "COM3", "COM6" };
        public static int[] baudrate = new int[] { 115200, 9600 };
        public static int[] numdatabits = new int[] { 8, 8 };
        public static Parity[] parity = new Parity[] { (Parity)Enum.Parse(typeof(Parity), "None", true), (Parity)Enum.Parse(typeof(Parity), "Odd", true) };
        public static StopBits[] stopbits = new StopBits[] { (StopBits)Enum.Parse(typeof(StopBits), "One", true), (StopBits)Enum.Parse(typeof(StopBits), "Two", true) };
        private static char[] serialReadBuffer = new char[4096];
        //private readonly object seriallock;// = new object();
        //private readonly AutoResetEvent serialsignal;// = new AutoResetEvent(false);
    }
}
