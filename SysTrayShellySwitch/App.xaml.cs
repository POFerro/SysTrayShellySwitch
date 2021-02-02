using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SysTrayShellySwitch
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SettingsViewModel Settings { get; private set; }

        public new static App Current => (App)Application.Current;

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(AppDomain.CurrentDomain.SetupInformation.ApplicationBase)
             .AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
             .AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysTrayShellySwitch", "appSettings.json"), optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var shellyConfig = new ConfigSettings();
            Configuration.Bind("shellySwitch", shellyConfig);
            this.Settings = new SettingsViewModel(shellyConfig);
        }

        private void ConfigureServices(IServiceCollection services)
        {
        }
    }
}
