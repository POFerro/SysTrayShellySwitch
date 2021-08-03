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

        public ShellyManager(bool autoRefresh)
        {
            this.autoRefresh = autoRefresh;
            if (autoRefresh)
                this.refreshTimer = new Timer(this.RefreshShelliesTimerCallback, null, 0, 3000);
            else
                this.refreshTimer = new Timer(this.RefreshShelliesTimerCallback);
        }

        public event EventHandler<ShelliesRefreshedEvent> ShelliesRefreshed;

        private List<ShellyInfo> _foundShellies = new List<ShellyInfo>();
        public List<ShellyInfo> FoundShellies
        {
            get { return _foundShellies; }
            protected set { SetProperty(ref _foundShellies, value); }
        }

        private Task refreshTask = Task.CompletedTask;
        private void RefreshShelliesTimerCallback(object state)
        {
            if (this.refreshTask.IsCompleted)
                this.refreshTask = this.RefreshShelliesData();
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

            this.ShelliesRefreshed?.Invoke(this, new ShelliesRefreshedEvent { FoundShellies = this.FoundShellies });
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

        protected async Task RefreshShellies()
        {
            Trace.WriteLine("Refreshing found shellies with mDNS");
            var responses = await ZeroconfResolver.ResolveAsync("_hap._tcp.local.");
            var shelies = responses
                .Where(host => host.Services["_hap._tcp.local."].Properties
                                   .Any(pSet => pSet.TryGetValue("md", out string mdValue) &&
                                                mdValue.StartsWith("shelly", StringComparison.OrdinalIgnoreCase)
                                       )
                       )
                .ToList();

            var newShelies = shelies
                    .Where(newShelly => !this.FoundShellies.Any(existing => existing.IPAddress == newShelly.IPAddress))
                    .Select(shelly => new ShellyInfo
                    {
                        IPAddress = shelly.IPAddress,
                        Name = shelly.DisplayName
                    });

            if (newShelies.Any())
                this.FoundShellies = this.FoundShellies.Concat(newShelies).ToList();
        }

        public void Dispose()
        {
            this.refreshTimer.Dispose();

            refreshTask.Wait();
        }
    }
}