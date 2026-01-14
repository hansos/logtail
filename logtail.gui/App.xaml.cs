using logtail.gui.Services;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace logtail.gui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private static readonly ILogger _logger = Log.ForContext<App>();

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureLogging();
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            base.OnStartup(e);
            _logger.Information("Application started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _logger.Information("Application exiting");
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static void ConfigureLogging()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Create default logs directory used by appsettings.json (relative to app base)
            var defaultLogDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(defaultLogDir);

            // Enable Serilog self-log to Debug output for troubleshooting sink issues
            SelfLog.Enable(msg => Debug.WriteLine($"[Serilog] {msg}"));

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
            _logger.Information("Logging configured");
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger.Error(e.Exception, "Unhandled exception in UI thread");
            e.Handled = true; // prevent crash; consider showing a dialog if desired
        }
    }

}
