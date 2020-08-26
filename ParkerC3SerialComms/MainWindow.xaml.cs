using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace ParkerC3SerialComms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        public MainWindow()
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        private void okClick(object sender, RoutedEventArgs e)
        {
            setComPort setcomport = new setComPort(0);
            setcomport.ShowDialog();
        }

        private static readonly Regex _regex = new Regex("[^0-9.-]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }

        private void openUI(object sender, RoutedEventArgs e)
        {
            UserInterfacer userinterfacer = new UserInterfacer(0);
            userinterfacer.ShowDialog();
        }
    }
}
