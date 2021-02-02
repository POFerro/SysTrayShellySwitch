using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SysTrayShellySwitch
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ShellySwitchViewModel ViewModel => (ShellySwitchViewModel)this.DataContext;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new ShellySwitchViewModel(App.Current.Settings);
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SettingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsView();
            window.ShowDialog();
        }

        private void NotifyIcon_PopupOpened(object sender, RoutedEventArgs e)
        {
            var t = this.ViewModel.RefreshShellyState();
        }
    }
}
