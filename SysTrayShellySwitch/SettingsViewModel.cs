using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POF.Common;
using POF.Shelly;

namespace SysTrayShellySwitch
{
    public class AvailableSwitch
    {
        public string BridgeName { get; set; }
        public string SwitchName { get; set; }

        public string Host { get; set; }
        public short ShellyButtonId { get; set; }
        public HAPType ShellyDeviceType { get; set; }
        public string Model { get; set; }
    }

    public class ConfigSettings
    {
        public string ShellyAddress { get; set; }
        public int ShellyButtonId { get; set; }
        public HAPType ShellyDeviceType { get; set; }
    }

    public class SettingsViewModel : BindableBase
    {
        private readonly ShellyManager shellyManager;

        public SettingsViewModel(ConfigSettings configSettings)
        {
            this.Configuration = configSettings;
            this.shellyManager = new ShellyManager(false);
        }

        public async Task LoadShellies()
        {
            await this.shellyManager.LoadShellies();
            this.AvailableSwitches = 
                this.shellyManager.FoundShellies
                              .SelectMany(device =>
                                    device.Components
                                          .OfType<ShellySwitch>()
                                          .Where(sw => sw.HAPSvcType != HAPSvcType.Disabled)
                                          .Select(sw => new AvailableSwitch
                                          {
                                              BridgeName = device.Name,
                                              SwitchName = sw.Name,
                                              Host = device.DeviceId, // Should be host but due to temporary bug it's being broadcast as deviceId: https://github.com/mongoose-os-apps/shelly-homekit/issues/627#issuecomment-817384943
                                              ShellyButtonId = sw.Id,
                                              ShellyDeviceType = sw.HAPType,
                                              Model = device.Model
                                          })
                                          )
                              .ToList();

            this.SelectedSwitch = this.AvailableSwitches
                .FirstOrDefault(sw => sw.Host == this.ShellyAddress &&
                                      sw.ShellyButtonId == this.ShellyButtonId &&
                                      sw.ShellyDeviceType == this.ShellyDeviceType
                                      );
        }

        protected ConfigSettings Configuration { get; set; }

        private List<AvailableSwitch> availableSwitches;
        public List<AvailableSwitch> AvailableSwitches
        {
            get { return availableSwitches; }
            set { SetProperty(ref availableSwitches, value); }
        }

        private AvailableSwitch selectedSwitch;
        public AvailableSwitch SelectedSwitch
        {
            get { return selectedSwitch; }
            set { SetProperty(ref selectedSwitch, value); }
        }

        public string ShellyAddress => this.Configuration.ShellyAddress;
        public int ShellyButtonId => this.Configuration.ShellyButtonId;
        public HAPType ShellyDeviceType => this.Configuration.ShellyDeviceType;


        private void SetConfiguration(string iPAddress, short shellyButtonId, HAPType shellyDeviceType)
        {
            string appSettingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysTrayShellySwitch", "appSettings.json");

            JObject settings;
            if (File.Exists(appSettingFile))
                settings = JObject.Parse(File.ReadAllText(appSettingFile));
            else
                settings = new JObject();

            var configuration = new ConfigSettings {
                ShellyAddress = iPAddress,
                ShellyButtonId = shellyButtonId,
                ShellyDeviceType = shellyDeviceType
            };
            settings["shellySwitch"] = JObject.FromObject(configuration);

            Directory.CreateDirectory(Path.GetDirectoryName(appSettingFile));
            File.WriteAllText(appSettingFile, settings.ToString());

            this.Configuration = configuration;
        }

        public event EventHandler ConfigurationChanged;

        public void SaveChanges()
        {
            SetConfiguration(SelectedSwitch.Host, SelectedSwitch.ShellyButtonId, SelectedSwitch.ShellyDeviceType);

            this.ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
