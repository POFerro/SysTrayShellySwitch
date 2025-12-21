using POF.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Zeroconf;

namespace POF.Shelly
{
    public class ShelliesRefreshedEvent : EventArgs
    {
        public List<ShellyInfo> FoundShellies { get; init; }
    }

    public class ShellyManager : BindableBase, IDisposable
    {
        private Timer refreshTimer;
        private bool autoRefresh;

        protected static readonly HttpClient http = new();

        public ShellyManager(bool autoRefresh)
        {
            this.autoRefresh = autoRefresh;
            if (autoRefresh)
                this.refreshTimer = new Timer(this.RefreshShelliesTimerCallback, null, 0, 3000);
            else
                this.refreshTimer = new Timer(this.RefreshShelliesTimerCallback);

            Trace.WriteLine($"ShellyManager was created");
        }

        public event EventHandler<ShelliesRefreshedEvent> ShelliesRefreshed;

        private List<ShellyInfo> _foundShellies = new List<ShellyInfo>();
        public List<ShellyInfo> FoundShellies
        {
            get { return _foundShellies; }
            protected set { SetProperty(ref _foundShellies, value); }
        }

        public VersionInformation AvailableVersion
        {
            get { return _availableVersion; }
            private set { SetProperty(ref _availableVersion, value); }
        }
        private VersionInformation _availableVersion;

        private bool updatesAvailable;
        public bool UpdatesAvailable
        {
            get { return updatesAvailable; }
            private set { SetProperty(ref updatesAvailable, value); }
        }

        private Task refreshTask = Task.CompletedTask;
        private bool running = false;
        private object runningLock = new object();
        private void RefreshShelliesTimerCallback(object state)
        {
            lock (this.runningLock)
            {
                if (this.running)
                    return;
                this.running = true;
            }
            try
            {
                this.refreshTask = this.RefreshShelliesData();
            }
            finally
            {
                this.running = false;
            }
        }

        private bool shouldRefreshShellies = true;
        private async Task RefreshShelliesData()
        {
            if (this.shouldRefreshShellies)
            {
                await this.RefreshShellies();
                this.shouldRefreshShellies = false;
            }

            await Task.WhenAll(this.FoundShellies.Select(shelly => shelly.RefreshInfo()));

            await CheckForUpdates();

            this.ShelliesRefreshed?.Invoke(this, new ShelliesRefreshedEvent { FoundShellies = this.FoundShellies });
        }

        public async Task CheckForUpdates()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://rojer.me/files/shelly/update.json"));

            var response = await http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var updateStr = await response.Content.ReadAsStringAsync();
                var resp = JsonDocument.Parse(updateStr);


                var versionInfo = resp.RootElement.EnumerateArray().OrderByDescending(v => Version.Parse(v[1].GetProperty("version").GetString())).Select(b => JsonSerializer.Deserialize<VersionInformation>(b[1].ToString())).First();
                var version = versionInfo.Version;

                foreach (var shelly in this.FoundShellies)
                {
                    shelly.CheckForUpdates(versionInfo);
                }

                this.AvailableVersion = versionInfo;
                this.UpdatesAvailable = this.FoundShellies.Any(s => s.UpdateAvailable);
            }
            else
            {
                Trace.WriteLine($"Error in check for updates: Status->" + response.StatusCode + ":" + response.ReasonPhrase);
            }
        }

        public async Task UpdateAll()
        {
            await Task.WhenAll(this.FoundShellies.Where(s => s.UpdateAvailable).Select(s => s.Update()));
        }

        public void ScheduleShelliesRefresh()
        {
            this.shouldRefreshShellies = true;
        }

        public async Task LoadShellies(bool loadInfo = true)
        {
            if (this.autoRefresh)
                throw new InvalidOperationException("Call to manually load Shellies when autoRefresh = true is not allowed");

            await this.RefreshShellies();
            if (loadInfo)
            {
                await Task.WhenAll(this.FoundShellies.Select(shelly => shelly.RefreshInfo()));
            }
        }

        public async Task<string> ResolveShellyIPAddress(string shellyName)
        {
            var shellies = await this.FindShellies();

            return shellies.Where(s => s.Name == shellyName).Select(s => s.IPAddress).FirstOrDefault();
        }

        protected async Task RefreshShellies()
        {
            Trace.WriteLine("Refreshing found shellies with mDNS");

            //ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();
            //Trace.WriteLine($"Found domains: \r\n{string.Join("\r\n", domains.Select(d => d.Key + "->" + string.Join(";", d)))}");

            //var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));
            //foreach (var resp in responses)
            //    Console.WriteLine(resp);

            var responses = await ZeroconfResolver.ResolveAsync("_hap._tcp.local.");
            Trace.WriteLine($"Found {responses.Count} devices for _hap.tcp.local.");

            var shelies = await this.FindShellies();

            var newShelies = shelies
                    .Where(newShelly => !this.FoundShellies.Any(existing => existing.IPAddress == newShelly.IPAddress))
                    ;

            this.FoundShellies = this.FoundShellies.Concat(newShelies).ToList();
        }

        protected async Task<IList<ShellyInfo>> FindShellies()
        {
            Trace.WriteLine("Finding shellies with mDNS");

            //ILookup<string, string> domains = await ZeroconfResolver.BrowseDomainsAsync();
            //Trace.WriteLine($"Found domains: \r\n{string.Join("\r\n", domains.Select(d => d.Key + "->" + string.Join(";", d)))}");

            //var responses = await ZeroconfResolver.ResolveAsync(domains.Select(g => g.Key));
            //foreach (var resp in responses)
            //    Console.WriteLine(resp);

            var responses = await ZeroconfResolver.ResolveAsync("_hap._tcp.local.");
            Trace.WriteLine($"Found {responses.Count} devices for _hap.tcp.local.");

            var shelies = responses
                .Where(host => host.Services.FirstOrDefault(s => s.Value.Name == "_hap._tcp.local.").Value?.Properties
                                   .Any(pSet => pSet.TryGetValue("md", out string mdValue) &&
                                                mdValue.StartsWith("shelly", StringComparison.OrdinalIgnoreCase)
                                       ) == true
                       )
                .Select(shelly => new ShellyInfo
                {
                    IPAddress = shelly.IPAddress,
                    Name = shelly.DisplayName
                })
                .ToList();
            Trace.WriteLine($"Found {shelies.Count} shelly devices");

            return shelies;
        }

        public void Dispose()
        {
            this.refreshTimer.Dispose();

            if (!new[] { TaskStatus.RanToCompletion, TaskStatus.Faulted, TaskStatus.Canceled }.Contains(refreshTask.Status))
                refreshTask.Wait();
            else 
                refreshTask.Dispose();
        }
    }
}