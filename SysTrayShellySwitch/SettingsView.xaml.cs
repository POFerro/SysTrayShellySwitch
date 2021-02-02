using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SysTrayShellySwitch
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        public SettingsViewModel ViewModel => (SettingsViewModel)this.DataContext;

        public SettingsView()
        {
            InitializeComponent();

            this.DataContext = App.Current.Settings;

            var t = this.ViewModel.LoadShellies();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.ViewModel.SaveChanges();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
