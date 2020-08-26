using System;
using System.Windows;
using System.IO.Ports;

namespace ParkerC3SerialComms
{
    /// <summary>
    /// Interaction logic for setComPort.xaml
    /// </summary>
    public partial class setComPort : Window
    {
        private int _portIndex;
        public setComPort(int portIndex)
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

            portHeading.Text += (_portIndex + 1);
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();

            // Display each port name to the console.
            foreach (string port in ports)
            {
                this.lstComPort.Items.Add(port);
            }

            txtComPortNum.Text = Globals.comport[_portIndex];
            txtBaudRate.Text = Globals.baudrate[_portIndex].ToString();
            txtNumDataBits.Text = Globals.numdatabits[_portIndex].ToString();
        }

        private void lstComPortChanged(object sender, RoutedEventArgs e)
        {
            SerialPort rsport = new SerialPort(lstComPort.SelectedItem.ToString());
            txtComPortNum.Text = lstComPort.SelectedItem.ToString();
            txtBaudRate.Text = rsport.BaudRate.ToString();
            txtNumDataBits.Text = rsport.DataBits.ToString();
            foreach (string s in Enum.GetNames(typeof(Parity)))
                lstParity.Items.Add(s);
            lstParity.SelectedIndex = 0;
            foreach (string s in Enum.GetNames(typeof(StopBits)))
                lstStopBits.Items.Add(s);
            lstStopBits.SelectedIndex = 1;
        }

        private void launchTerminalEmulator(object sender, RoutedEventArgs e)
        {
            terminalEmulator tmEmulator = new terminalEmulator(_portIndex);
            tmEmulator.ShowDialog();
        }

        private void okClick(object sender, RoutedEventArgs e)
        {
            Globals.comport[_portIndex] = txtComPortNum.Text;
            if (!Int32.TryParse(txtBaudRate.Text, out Globals.baudrate[_portIndex]))
                Globals.baudrate[_portIndex] = 0;
            if (!Int32.TryParse(txtNumDataBits.Text, out Globals.numdatabits[_portIndex]))
                Globals.numdatabits[_portIndex] = 0;
            if (lstParity.SelectedIndex >= 0 && lstParity.SelectedItem.ToString() != null)
                Globals.parity[_portIndex] = (Parity)Enum.Parse(typeof(Parity), lstParity.SelectedItem.ToString(), true);
            else
                Globals.parity[_portIndex] = (Parity)Enum.Parse(typeof(Parity), "None", true);
            if (lstStopBits.SelectedIndex >= 0 && lstStopBits.SelectedItem.ToString() != "")
                Globals.stopbits[_portIndex] = (StopBits)Enum.Parse(typeof(StopBits), lstStopBits.SelectedItem.ToString(), true);
            else
                Globals.stopbits[_portIndex] = (StopBits)Enum.Parse(typeof(StopBits), "None", true);
            //this.Close();
        }
        private void cancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
