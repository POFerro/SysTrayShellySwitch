using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POF.Common;
using POF.Shelly;

namespace SysTrayShellySwitch
{
    public class ShellySwitchViewModel : BindableBase
    {
        public ShellyInfo ShellyDevice { get; private set; }
        public ShellySwitch ShellySwitch
        {
            get { return shellySwitch; }
            private set { SetProperty(ref shellySwitch, value); }
        }
        private ShellySwitch shellySwitch;

        private string shellyIPAddress;

        private SettingsViewModel settings;
        public ShellySwitchViewModel(SettingsViewModel settings)
        {
            this.settings = settings;
            this.ShellyDevice = new ShellyInfo();

            this.settings.ConfigurationChanged += ConfigurationChanged_Handler;

            var t = this.Initialize();
        }

        private async Task Initialize()
        {

            this.shellyIPAddress = await this.settings.GetShellyIPAddress();
            await this.RefreshDevice();
        }

        private void ConfigurationChanged_Handler(object sender, EventArgs e)
        {
            var t = this.RefreshDevice();
        }

        private async Task RefreshDevice()
        {
            this.ShellyDevice.IPAddress = this.shellyIPAddress;

            await this.ShellyDevice.RefreshInfo();

            this.ShellySwitch = this.ShellyDevice.Components
                                    .Where(c => c.Id == settings.ShellyButtonId && c.HAPType == settings.ShellyDeviceType)
                                    .OfType<ShellySwitch>()
                                    .FirstOrDefault()
                                    ;
            if (this.ShellySwitch == null)
            {
                Trace.TraceWarning("Device was not found, retrying in 1 sec");
                
                await Task.Delay(1000);

                await RefreshDevice();
            }
            else
                this.ShellySwitch.StateStringFormatter = (state) => $"As luzes estão {(state ? "ligadas" : "desligadas")}";
        }

        public Task RefreshShellyState()
        {
            return this.RefreshDevice();
        }
    }
}
