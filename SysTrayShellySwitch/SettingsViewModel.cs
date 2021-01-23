using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POF.WPF;

namespace SysTrayShellySwitch
{
    public class SettingsViewModel : BindableBase
    {
        private readonly ShellySwitchViewModel mainViewModel;

        private static readonly Dictionary<int, string> availableButtons = new Dictionary<int, string>() {
            { 1, "Botão único ou botão esquerdo" },
            { 2, "Botão direito" }
        };

        public IEnumerable<KeyValuePair<int, string>> AvailableButtons => availableButtons.AsEnumerable();

        public SettingsViewModel(ShellySwitchViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;

            this.ShellyAddress = this.mainViewModel.ShellyAddress;
            this.ShellyButtonId = this.mainViewModel.ShellyButtonId;
        }

        private string shellyAddress = "http://switchescritorio.local";
        public string ShellyAddress
        {
            get { return shellyAddress; }
            set
            {
                SetProperty(ref shellyAddress, value);
            }
        }

        private int shellyButtonId = 1;
        public int ShellyButtonId
        {
            get { return shellyButtonId; }
            set
            {
                SetProperty(ref shellyButtonId, value);
            }
        }

        public void SaveChanges()
        {
            this.mainViewModel.SetShellyConfiguration(this.ShellyAddress, this.ShellyButtonId);
        }
    }
}
