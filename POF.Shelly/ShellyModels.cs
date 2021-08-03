﻿using POF.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace POF.Shelly
{
    public enum SysMode : short
    {
        Switch = 0,
        RollerShutter = 1,
        GarageDoorOpener = 2
    }

    public enum HAPSvcType : short
    {
        // Switch
        Disabled = -1,
        Switch = 0,
        Outlet = 1,
        Lock = 2,
    }

    public enum HAPType : short
    {
        Switch = 0,
        Outlet = 1,
        Lock = 2,

        ProgramableSwitch = 3,

        ShellyWindowCovering = 4,
        ShellyGarageDoorOpener = 5,

        DisabledInput = 6,
        MotionSensor = 7,
        OccupancySensor = 8,
        ContactSensor = 9
    }

    public enum SwitchInputMode : short
    {
        Momentary = 0,
        Toggle = 1,
        Edge = 2,
        Detached = 3,
        Activation = 4
    }

    public enum StatelessSwitchInputMode : short
    {
        Momentary = 0,
        ToggleOnOffSinglePress = 1,
        ToggleOnSingleOffDoublePress = 2,
    }

    public enum SensorInputMode : short
    {
        Level = 0,
        Pulse = 1
    }

    public enum WindowCoveringInputMode : short
    {
        SeparateMomentary = 0,
        SeparateToggle = 1,
        Single = 2,
        Detached = 3
    }

    public enum InitialState : short
    {
        Off = 0,
        On = 1,
        Last = 2,
        Input = 3
    }

    public class ShellyComponentJsonSerializer : PolymorphicConverter<ShellyComponent, short>
    {
        public ShellyComponentJsonSerializer()
            : base("type", new Dictionary<short, Type> {
                { 0, typeof(ShellySwitch) }, // Switch
                { 1, typeof(ShellySwitch) }, // Outlet
                { 2, typeof(ShellySwitch) }, // Lock
                
                { 3, typeof(ShellyProgramableInput) },

                { 4, typeof(ShellyWindowCovering) },

                { 5, typeof(ShellyGarageDoorOpener) },

                { 6, typeof(ShellyDisabledInput) },

                { 7, typeof(ShellySensor) }, // MotionSensor
                { 8, typeof(ShellySensor) }, // OccupancySensor
                { 9, typeof(ShellySensor) }, // ContactSensor
            })
        {

        }

        protected override short ReadDisciminatorValue(ref Utf8JsonReader reader)
        {
            return reader.GetInt16();
        }
    }

    [JsonConverter(typeof(ShellyComponentJsonSerializer))]
    public class ShellyComponent : BindableBase
    {

        [JsonPropertyName("id")]
        public short Id { get; set; }

        [JsonPropertyName("type")]
        public HAPType HAPType { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        protected static readonly HttpClient http = new();

        // For serialization
        protected ShellyComponent()
        {
        }

        public ShellyComponent(ShellyInfo parent)
        {
            this.SetParent(parent);
        }
        public ShellyInfo Parent { get; private set; }
        internal void SetParent(ShellyInfo parent)
        {
            this.Parent = parent;
        }
    }

    public class ShellySwitch : ShellyComponent
    {
        protected ShellySwitch()
        {
        }

        public ShellySwitch(ShellyInfo parent)
            : base(parent)
        {
        }

        [JsonPropertyName("svc_type")]
        public HAPSvcType HAPSvcType { get; set; }

        [JsonPropertyName("state")]
        public bool State
        {
            get { return _state; }
            set
            {
                SetProperty(ref _state, value);
                SetProperty(ref switchState, value);
                OnPropertyChanged(nameof(this.SwitchState));
                OnPropertyChanged(nameof(this.StateStr));
            }
        }
        private bool _state;

        [JsonIgnore()]
        public bool SwitchState
        {
            get { return switchState; }
            set
            {
                if (value != this.State)
                {
                    SaveState(value)
                        .ContinueWith((t, state) =>
                        {
                            this.State = (bool)state; // Altera o switchState tb
                        },
                            value, TaskScheduler.Current
                        );
                }
            }
        }
        private bool switchState;

        public string StateStr => this.StateStringFormatter(this.State);

        public Func<bool, string> StateStringFormatter
        {
            get { return _stateStringFormatter; }
            set
            {
                SetProperty(ref _stateStringFormatter, value);
                OnPropertyChanged(nameof(StateStr));
            }
        }
        private Func<bool, string> _stateStringFormatter = (state) => state switch
                   {
                       true => "Ligado",
                       false => "Desligado"
                   };


        [JsonPropertyName("in_mode")]
        public SwitchInputMode InputMode { get; set; }

        [JsonPropertyName("in_inverted")]
        public bool InInverted { get; set; }

        [JsonPropertyName("initial")]
        public InitialState InitialState { get; set; }

        [JsonPropertyName("auto_off")]
        public bool? AutoOff { get; set; }

        [JsonPropertyName("auto_off_delay")]
        public decimal? AutoOffDelay { get; set; }

        [JsonPropertyName("apower")]
        public decimal? APower
        {
            get { return _aPower; }
            set { SetProperty(ref _aPower, value); }
        }
        private decimal? _aPower;

        [JsonPropertyName("aenergy")]
        public decimal? AEnergy
        {
            get { return _aEnergy; }
            set { SetProperty(ref _aEnergy, value); }
        }
        private decimal? _aEnergy;


        protected virtual async Task SaveState(bool value)
        {
            var apiAddress = new Uri(new Uri("http://" + this.Parent.IPAddress),
                $"rpc/Shelly.SetState?id={this.Id}&type={(short)this.HAPType}&state=%7b%22state%22%3a{JsonSerializer.Serialize(value)}%7d");

            var response = await http.GetAsync(apiAddress);
        }
        //http
    }


    public class ShellyProgramableInput : ShellyComponent
    {
        [JsonPropertyName("in_mode")]
        public StatelessSwitchInputMode InputMode { get; set; }

        [JsonPropertyName("inverted")]
        public bool Inverted { get; set; }

        [JsonPropertyName("last_ev")]
        public short? LastEvent
        {
            get { return _lastEvent; }
            set { SetProperty(ref _lastEvent, value); }
        }
        private short? _lastEvent;

        [JsonPropertyName("last_ev_age")]
        public decimal? LastEventAge { get; set; }
    }

    #region Desinteressantes
    public class ShellyWindowCovering : ShellyComponent
    {
        [JsonPropertyName("in_mode")]
        public WindowCoveringInputMode InputMode { get; set; }

        [JsonPropertyName("swap_inputs")]
        public bool SwapInputs { get; set; }
        [JsonPropertyName("swap_outputs")]
        public bool SwapOutputs { get; set; }

        [JsonPropertyName("state")]
        public int State { get; set; }

        [JsonPropertyName("state_str")]
        public string StateString { get; set; }

        [JsonPropertyName("cur_pos")]
        public string CurrentPosition { get; set; }
        [JsonPropertyName("tgt_pos")]
        public string TargetPosition { get; set; }

        [JsonPropertyName("cal_done")]
        public bool CalibrationDone { get; set; }


        [JsonPropertyName("move_time_ms")]
        public long MoveTimeMs { get; set; }

        [JsonPropertyName("move_power")]
        public long MovePower { get; set; }
    }


    public enum CloseSensorMode
    {
        NormallyClosed = 0,
        NormallyOpen = 1,
    }
    public enum OpenSensorMode
    {
        NormallyClosed = 0,
        NormallyOpen = 1,
        Disabled = 2
    }
    public enum OutputMode
    {
        Single = 0,
        Dual = 1
    }

    public class ShellyGarageDoorOpener : ShellyComponent
    {

        [JsonPropertyName("cur_state_str")]
        public string CurrentStateStr { get; set; }

        [JsonPropertyName("close_sensor_mode")]
        public CloseSensorMode CloseSensorMode { get; set; }

        [JsonPropertyName("open_sensor_mode")]
        public OpenSensorMode OpenSensorMode { get; set; }

        [JsonPropertyName("out_mode")]
        public OutputMode OutputMode { get; set; }


        [JsonPropertyName("move_time")]
        public long MovementTime { get; set; }

        [JsonPropertyName("pulse_time_ms")]
        public long PulseTimeMs { get; set; }
    }

    public class ShellyDisabledInput : ShellyComponent
    {
    }

    public class ShellySensor : ShellyComponent
    {
        [JsonPropertyName("inverted")]
        public bool Inverted { get; set; }

        [JsonPropertyName("in_mode")]
        public SensorInputMode InputMode { get; set; }

        [JsonPropertyName("idle_time")]
        public decimal IdleTime { get; set; }

        [JsonPropertyName("state")]
        public bool State { get; set; }

        [JsonPropertyName("last_ev_age")]
        public decimal? LastEventAge { get; set; }
    }

    #endregion


    public class ShellyInfo : BindableBase
    {
        [JsonPropertyName("app")]
        public string App { get; set; }
        [JsonPropertyName("device_id")]
        public string DeviceId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("stock_model")]
        public string StockModel { get; set; }

        [JsonPropertyName("host")]
        public string Host
        {
            get { return _host; }
            set
            {
                SetProperty(ref _host, value);
                OnPropertyChanged(nameof(HostUri));
            }
        }
        private string _host;

        public Uri HostUri => this.Host != null ? new Uri("http://" + this.Host) : null;

        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("fw_build")]
        public string FWBuild { get; set; }
        [JsonPropertyName("uptime")]
        public long UpTime
        {
            get { return _upTime; }
            set { SetProperty(ref _upTime, value); }
        }
        private long _upTime;
        [JsonPropertyName("failsafe_mode")]
        public bool FailsafeMode { get; set; }
        [JsonPropertyName("wifi_en")]
        public bool WifiClientEnabled { get; set; }
        [JsonPropertyName("wifi_ssid")]
        public string WifiSSID { get; set; }
        [JsonPropertyName("wifi_pass")]
        public string WifiPass { get; set; }
        [JsonPropertyName("wifi_rssi")]
        public long WifiRSSI { get; set; }
        [JsonPropertyName("wifi_ip")]
        public string IPAddress { get; set; }
        [JsonPropertyName("hap_cn")]
        public short HAPConnections { get; set; }
        [JsonPropertyName("hap_running")]
        public bool HAPRunning { get; set; }
        [JsonPropertyName("hap_paired")]
        public bool HAPPaired { get; set; }
        [JsonPropertyName("hap_ip_conns_pending")]
        public short HAPPendingConnections { get; set; }
        [JsonPropertyName("hap_ip_conns_active")]
        public short HAPActiveConnections { get; set; }
        [JsonPropertyName("hap_ip_conns_max")]
        public short HAPMaxConnections { get; set; }
        [JsonPropertyName("sys_mode")]
        public SysMode SysMode { get; set; }
        [JsonPropertyName("rsh_avail")]
        public bool RshAvail { get; set; }
        [JsonPropertyName("gdo_avail")]
        public bool GdoAvail { get; set; }
        [JsonPropertyName("debug_en")]
        public bool DebugEn { get; set; }
        [JsonPropertyName("sys_temp")]
        public short SysTemperature
        {
            get { return _sysTemperature; }
            set { SetProperty(ref _sysTemperature, value); }
        }
        private short _sysTemperature;

        [JsonPropertyName("overheat_on")]
        public bool OverheatOn { get; set; }

        [JsonPropertyName("components")]
        public List<ShellyComponent> Components
        {
            get { return _components; }
            set { SetProperty(ref _components, value); }
        }
        private List<ShellyComponent> _components = new List<ShellyComponent>();

        [JsonIgnore()]
        public HttpErrorDetails ReadInfoError
        {
            get { return _readInfoEror; }
            private set { SetProperty(ref _readInfoEror, value); }
        }
        private HttpErrorDetails _readInfoEror;

        protected static readonly HttpClient http = new();
        public ShellyInfo()
        {
        }

        public async Task RefreshInfo()
        {
            var response = await http.GetAsync(new Uri($"http://{this.IPAddress}/rpc/Shelly.GetInfoExt"));
            if (response.IsSuccessStatusCode)
            {
                this.ReadInfoError = null;

                var deviceInfoStr = await response.Content.ReadAsStringAsync();
                var newInfo = JsonSerializer.Deserialize<ShellyInfo>(deviceInfoStr);

                CopyFrom(newInfo);
            }
            else
            {
                ReadInfoError = !response.IsSuccessStatusCode ? new HttpErrorDetails { StatusCode = response.StatusCode, ReasonPhrase = response.ReasonPhrase, ErrorMessage = await response.Content.ReadAsStringAsync() }
                                                              : null;
            }
        }

        public void CopyFrom(ShellyInfo newInfo)
        {
            foreach (var prop in this.GetType().GetProperties()
                                               .Where(p => p.CanRead && p.CanWrite)
                                               .Where(p => p.Name != nameof(Components)))
            {
                prop.SetValue(this, prop.GetValue(newInfo));
            }

            foreach (var comp in newInfo.Components)
            {
                comp.SetParent(this);
            }

            var removedComponents = this.Components
                .Where(existing => !newInfo.Components
                                           .Any(newComp => existing.Id == newComp.Id && existing.HAPType == newComp.HAPType)
                      )
                .ToList();
            if (removedComponents.Count > 0)
            {
                this.Components = this.Components.Except(removedComponents).ToList();
            }

            if (this.Components.Count == 0)
            {
                this.Components = newInfo.Components;
            }
            else
            {
                var components = newInfo.Components
                    .GroupJoin(this.Components, c => new { c.Id, c.HAPType }, c => new { c.Id, c.HAPType }, (newInfo, oldInfo) => new { newInfo, oldInfo = oldInfo.FirstOrDefault() })
                    .ToList();
                foreach (var compo in components.Where(c => c.oldInfo != null))
                {
                    foreach (var prop in compo.oldInfo.GetType().GetProperties()
                                                        .Where(p => p.CanRead && p.CanWrite))
                    {
                        prop.SetValue(compo.oldInfo, prop.GetValue(compo.newInfo));
                    }
                }
                if (components.Any(c => c.oldInfo == null))
                {
                    this.Components = this.Components.Concat(components.Where(c => c.oldInfo == null).Select(c => c.newInfo)).ToList();
                }
            }
        }
    }
}
