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
    public class ShellySwitchViewModel : BindableBase
    {
        static HttpClient http = new HttpClient();

        public ShellySwitchViewModel()
        {
            var t = this.RefreshShellyState();
        }

        private string shellyAddress = "http://switchescritorio.local";
        public string ShellyAddress
        {
            get { return shellyAddress; }
            set
            {
                SetProperty(ref shellyAddress, value);
                var t = RefreshShellyState();
            }
        }

        private int shellyButtonId = 1;
        public int ShellyButtonId
        {
            get { return shellyButtonId; }
            set
            {
                SetProperty(ref shellyButtonId, value);
                var t = RefreshShellyState();
            }
        }

        public string SwitchStateToolTip
        {
            get { return $"As luzes estão {(this.SwitchState ? "ligadas" : "desligadas")}"; }
        }

        private bool switchState;
        public bool SwitchState
        {
            get { return switchState; }
            set
            {
                //SetProperty(ref switchState, value);
                //OnPropertyChanged(nameof(SwitchStateToolTip));

                //SetShellyState(value);
                SetShellyState(value)
                    .ContinueWith((t, state) =>
                    {
                        SetProperty(ref switchState, (bool)state);
                        OnPropertyChanged(nameof(SwitchStateToolTip));
                    },
                        value, TaskScheduler.Current
                    );
            }
        }

        public void SetShellyConfiguration(string shellyAddress, int shellyButtonId)
        {
            this.shellyAddress = shellyAddress;
            this.shellyButtonId = shellyButtonId;

            OnPropertyChanged(nameof(ShellyAddress));
            OnPropertyChanged(nameof(ShellyButtonId));

            var t = RefreshShellyState();
        }

        public virtual async Task RefreshShellyState()
        {
            this.SwitchState = await GetShellyState();
        }

        protected virtual async Task SetShellyState(bool value)
        {
            var apiAddress = new Uri(new Uri(this.ShellyAddress),
                $"rpc/Shelly.SetState?id={ShellyButtonId}&type=0&state=%7b%22state%22%3a{JsonConvert.ToString(value)}%7d");

            var response = await http.GetAsync(apiAddress);
        }

        protected virtual async Task<bool> GetShellyState()
        {
            var response = await http.GetAsync(new Uri(new Uri(this.ShellyAddress), $"rpc/Shelly.GetInfo?id={ShellyButtonId}&type=0"));
            await AssureSuccessfullResponse(response);

            var responseString = await response.Content.ReadAsStringAsync();
            var responseJson = JObject.Parse(responseString);
            return responseJson["components"]
                    .First(c => c.Value<int>("id") == this.ShellyButtonId)
                    .Value<bool>("state");
        }

        protected async Task AssureSuccessfullResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Http error: {response.ReasonPhrase}->{responseContent}", null, response.StatusCode);
            }
        }
    }
}
