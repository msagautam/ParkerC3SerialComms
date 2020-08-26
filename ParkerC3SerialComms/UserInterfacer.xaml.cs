using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace ParkerC3SerialComms
{
    /// <summary>
    /// Interaction logic for terminalEmulator.xaml
    /// </summary>
    public partial class UserInterfacer : Window
    {
        private int _portIndex;

        static Queue<(string,string)> _serialSendQueue = new Queue<(string, string)>();
        static Queue<string> _serialSentQueue = new Queue<string>();
        static Queue<string> _serialRecvdQueue = new Queue<string>();
        static bool _continue = false;
        static SerialPort _serialPort;
        Thread appendtxtThread, writeThread, dispThread;
        private readonly object _lock = new object();
        private readonly AutoResetEvent _serialRecvdSignal = new AutoResetEvent(false);
        string recvdmsg;
        DispatcherTimer dispatcherTimer;

        public UserInterfacer(int portIndex)// (String txtComPortNum, String txtNumDataBits, String txtParity, String txtStopBits)
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

            writeThread = new Thread(SerialWriteThreadFunc);
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialReadEvent);
            _serialPort.ErrorReceived += new SerialErrorReceivedEventHandler(SerialReadErr);
            //_serialPort.Handshake = Handshake.RequestToSend;
            //_serialPort.RtsEnable = true;
            //_serialPort.DtrEnable = true;
            appendtxtThread = new Thread(AppendSerialMessages);
            dispThread = new Thread(DisplaySerialMessages);

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(parkerStatusQuery);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);//TimeSpan(0, 0, 2); //
            dispatcherTimer.Start();//Remove if using StartMonitor button

            try
            {
                _serialPort.Open();
                _continue = true;
                appendtxtThread.Start();
                writeThread.Start();
                dispThread.Start();
            }
            catch (Exception ex)
            {
                rsBox.Text += String.Format("Opening Serial Port Failed! May be the Com port is not valid or is being used by different program.\r\n{0}\r\n", ex.ToString());
            }
        }

        private void parkerStatusQuery(object sender, EventArgs e)
        {
            _serialSendQueue.Enqueue(("PStatus", String.Format("o1901.1\r\n")));
            _serialSendQueue.Enqueue(("VStatus", String.Format("o1902.1\r\n")));
            _serialSendQueue.Enqueue(("AStatus", String.Format("o1906.1\r\n")));
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        private void DistanceSend(object sender, RoutedEventArgs e)
        {
            _serialSendQueue.Enqueue(("PSet", String.Format("o1901.1={0}\r\n", txtDistance.Text)));
        }

        private void VelocitySend(object sender, RoutedEventArgs e)
        {
            _serialSendQueue.Enqueue(("VSet", String.Format("o1902.1={0}\r\n", txtVel.Text)));
        }

        private void AccelerationSend(object sender, RoutedEventArgs e)
        {
            _serialSendQueue.Enqueue(("ASet", String.Format("o1906.1={0}\r\n", txtAccel.Text)));
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
                recvdmsg += indata;
                _serialRecvdSignal.Set();
            }
        }
        private void SerialWriteThreadFunc()
        {
            while (_continue)
            {
                if (_serialSendQueue.Count > 0)
                {
                    (string qtype, string query) = _serialSendQueue.Dequeue();
                    _serialPort.WriteLine(query);
                    _serialSentQueue.Enqueue(qtype);
                }
                Thread.Sleep(10);
            }
        }

        public void AppendSerialMessages()
        {
            while (_continue)
            {
                _serialRecvdSignal.WaitOne();
                lock (_lock)
                {
                    //int dindex = recvdmsg.IndexOf((char)0x0D);
                    String[] statusstrs = recvdmsg.Split((char)0x0D);
                    for (int i=0; i<statusstrs.Length-1; i++)
                        _serialRecvdQueue.Enqueue(statusstrs[i]);
                    recvdmsg = statusstrs[statusstrs.Length - 1];                    
                }
            }
        }

        public void DisplaySerialMessages()
        {
            byte[] strbytes;
            string dispmsg, disptype;
            while (_continue)
            {
                if (_serialRecvdQueue.Count > 0)
                {
                    this.Dispatcher.Invoke(
                                 DispatcherPriority.Normal,
                                 (ThreadStart)delegate
                                 {
                                     dispmsg = _serialRecvdQueue.Dequeue();
                                     disptype = _serialSentQueue.Dequeue();
                                     if (disptype == "PStatus") txtPosStatus.Content = dispmsg;
                                     else if (disptype == "VStatus") txtVelStatus.Content = dispmsg;
                                     else if (disptype == "AStatus") txtAccStatus.Content = dispmsg;

                                     //Console.WriteLine("test");
                                     //rsBox.Text += "(" + recvdmsg + ") " + _serialSentQueue.Dequeue() + " : " + dispmsg + " (";
                                     rsBox.AppendText("(" + recvdmsg + ") ");
                                     rsBox.AppendText(disptype + " : " + dispmsg + " (");
                                     strbytes = Encoding.Default.GetBytes(dispmsg);
                                     //rsBox.Text += BitConverter.ToString(strbytes) + ")\r\n\n";
                                     rsBox.AppendText(BitConverter.ToString(strbytes) + ")\r\n");
                                     rsBox.ScrollToEnd();
                                 }
                             );
                }
            }
        }
        private void StartMonitor(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Start();
        }

        private void StopMonitor(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
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