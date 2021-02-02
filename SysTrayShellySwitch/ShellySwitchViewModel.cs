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


        private SettingsViewModel settings;
        public ShellySwitchViewModel(SettingsViewModel settings)
        {
            this.settings = settings;
            this.ShellyDevice = new ShellyInfo();

            this.settings.ConfigurationChanged += ConfigurationChanged_Handler;

            this.RefreshDevice();
        }

        private void ConfigurationChanged_Handler(object sender, EventArgs e)
        {
            this.RefreshDevice();
        }

        private Task RefreshDevice()
        {
            this.ShellyDevice.IPAddress = settings.ShellyAddress;

            return this.ShellyDevice.RefreshInfo().ContinueWith((task) => {
                this.ShellySwitch = this.ShellyDevice.Components
                                        .Where(c => c.Id == settings.ShellyButtonId && c.HAPType == settings.ShellyDeviceType)
                                        .OfType<ShellySwitch>()
                                        .FirstOrDefault()
                                        ;
                if (this.ShellySwitch == null)
                {
                    Trace.TraceWarning("Device was not found, retrying in 1 sec");
                    Task.Delay(1000).ContinueWith(task => RefreshDevice());
                }
                else
                    this.ShellySwitch.StateStringFormatter = (state) => $"As luzes estão {(state ? "ligadas" : "desligadas")}";
            });
        }

        public Task RefreshShellyState()
        {
            return this.RefreshDevice();
        }
    }
}
