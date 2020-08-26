using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ParkerC3SerialComms
{
    /// <summary>
    /// Interaction logic for UserInterface.xaml
    /// </summary>
    public partial class terminalEmulator : Window
    {
        private int _portIndex;

        static bool _continue = false;
        static SerialPort _serialPort;
        Thread appendtxtThread, readThread;
        private readonly object _lock = new object();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        string recvdmsg;

        public terminalEmulator(int portIndex)// (String txtComPortNum, String txtNumDataBits, String txtParity, String txtStopBits)
        {
            _portIndex = portIndex;
            InitializeComponent();
        }

        private void onload(object sender, RoutedEventArgs e)
        {
            Application curApp = Application.Current;
            Window mainWindow = curApp.MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;

            this.rsBox.Text += "************************************************************************\r\n";
            this.rsBox.Text = Globals.comport[_portIndex] + "\r\n";
            this.rsBox.Text += Globals.baudrate[_portIndex] + "\r\n";
            this.rsBox.Text += Globals.parity[_portIndex] + "\r\n";
            this.rsBox.Text += Globals.numdatabits[_portIndex] + "\r\n";
            this.rsBox.Text += Globals.stopbits[_portIndex] + "\r\n";
            this.rsBox.Text += "************************************************************************\r\n";

            _serialPort = new SerialPort(Globals.comport[_portIndex], Globals.baudrate[_portIndex], Globals.parity[_portIndex], Globals.numdatabits[_portIndex], Globals.stopbits[_portIndex]);
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            readThread = new Thread(SerialReadThreadFunc);
            //_serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialReadEvent);
            //_serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(SerialReadErr);
            //_serialPort.Handshake = Handshake.RequestToSend;
            //_serialPort.RtsEnable = true;
            //_serialPort.DtrEnable = true;
            appendtxtThread = new Thread(AppendSerialMessages);

            try
            {
                _serialPort.Open();
                _continue = true;
                appendtxtThread.Start();
                readThread.Start();
            }
            catch (Exception ex)
            {
                rsBox.Text += String.Format("Opening Serial Port Failed! May be the Com port is not valid or is being used by different program.\r\n{0}\r\n", ex.ToString());
            }
        }

        private void SerialSend(object sender, RoutedEventArgs e)
        {
            _serialPort.WriteLine(
                    String.Format("{0}\r\n", sendmsg.Text));
            //_serialPort.WriteLine(
            //        String.Format("<{0}>: {1}\r\n", "PC", sendmsg.Text));
        }

        private void SerialReadErr(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("Error Received:");
            Console.Write(e.ToString());
        }
        private void SerialReadEvent(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            //Console.WriteLine("Data Received:");
            //Console.Write(indata);
            lock (_lock)
            {
                recvdmsg = indata;
                _signal.Set();
            }
        }

        private void SerialReadThreadFunc()
        {
            while (_continue)
            {

                if (_serialPort.BytesToRead > 0)
                {
                    try
                    {
                        lock (_lock)
                        {
                            recvdmsg = _serialPort.ReadLine();
                            //Console.WriteLine("test");
                            _signal.Set();
                        }
                    }
                    catch (TimeoutException) { Console.WriteLine(String.Format("<NewLine not recvd after {0}> bytes: {1}\r\n", _serialPort.BytesToRead, DateTimeOffset.Now.Millisecond)); }
                }
                else
                {
                    Console.WriteLine("Serial Buffer Empty: {0}\r\n", DateTimeOffset.Now.Millisecond);
                    Thread.Sleep(500);
                }
            }
        }

        public void AppendSerialMessages()
        {
            byte[] strbytes;
            while (_continue)
            {
                _signal.WaitOne();
                lock (_lock)
                {
                    this.Dispatcher.Invoke(
                             DispatcherPriority.Normal,
                             (ThreadStart)delegate {
                                 strbytes = Encoding.Default.GetBytes(recvdmsg);
                                 //Console.WriteLine("test");
                                 rsBox.Text += recvdmsg;
                                 rsBox.Text += BitConverter.ToString(strbytes) + "\r\n\n";
                             }
                         );
                }
            }
        }

        private void saveRsBox(object sender, RoutedEventArgs e)
        {

        }

        private void closeWindow(object sender, RoutedEventArgs e)
        {
            //readThread.Abort();
            _continue = false;
            _serialPort.Close();
            appendtxtThread.Abort();
            this.Hide();
            this.Close();
        }
    }
}