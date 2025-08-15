using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineStatusLight.Core.Configuration;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;
using System.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace OnlineStatusLight.Source.WindowsAutomation
{
    public class WindowsAutomationService : IMicrosoftTeamsService
    {
        private readonly ILogger<WindowsAutomationService> _logger;
        private readonly SourceWindowsAutomationConfiguration _windowsAutomationConfiguration;
        private MicrosoftTeamsStatus _lastStatus = MicrosoftTeamsStatus.Unknown;

        public WindowsAutomationService(
            IOptions<SourceWindowsAutomationConfiguration> windowsAutomationConfiguration,
            ILogger<WindowsAutomationService> logger)
        {
            _logger = logger;

            if (windowsAutomationConfiguration == null)
                throw new ConfigurationErrorsException("Configuration not found for source service WindowsAutomationConfiguration.");

            _windowsAutomationConfiguration = windowsAutomationConfiguration.Value;
            PoolingInterval = _windowsAutomationConfiguration.Interval;
        }

        public int PoolingInterval { get; set; }

        public async Task<MicrosoftTeamsStatus> GetCurrentStatus(CancellationToken cancellationToken)
        {
            var presenceStatus = "Unknown";
            var statusPattern = _windowsAutomationConfiguration.StatusPattern?.Replace("@status", "(.+)") ?? "";
            var restartProcess = _windowsAutomationConfiguration.RestartProcess;
            var restartArgument = _windowsAutomationConfiguration.RestartArgument;

            try
            {
                var rootElement = await Task.Run(() => AutomationElement.RootElement, cancellationToken);
                AutomationElement? teamsWindow = null;
                var processes = Process.GetProcessesByName(_windowsAutomationConfiguration.ProcessName);
                foreach (var process in processes)
                {
                    // if window is not minimized, we can use its MainWindowHandle
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        teamsWindow = AutomationElement.FromHandle(process.MainWindowHandle);
                        if (teamsWindow != null)
                            break;
                    }
                    // if window is minimized, we can use its MainModule.FileName and start the process minimized
                    else
                    {
                        if (restartProcess)
                        {
                            string? teamsPath = process?.MainModule?.FileName;
                            if (teamsPath != null)
                            {
                                var startInfo = new ProcessStartInfo
                                {
                                    FileName = teamsPath,
                                    WindowStyle = ProcessWindowStyle.Minimized,
                                    UseShellExecute = true,
                                    Arguments = restartArgument
                                };

                                try
                                {
                                    _logger.LogInformation("Restarting Teams process");
                                    Process.Start(startInfo);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error restarting Teams process: {Message}", ex.Message);
                                }
                            }
                        }

                        return _lastStatus;
                    }
                }

                if (teamsWindow == null)
                    return MicrosoftTeamsStatus.Unknown;

                // Look for the presence status element within the Teams window
                var presenceElements = await Task.Run(() =>
                {
                    var presenceCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                    return teamsWindow.FindAll(TreeScope.Descendants, presenceCondition);
                }, cancellationToken);

                foreach (AutomationElement element in presenceElements)
                {
                    // look for the string: "Your profile, status "
                    // and then look at the next words, which is my current status.
                    if (!string.IsNullOrEmpty(element.Current.Name))
                    {
                        var match = Regex.Match(element.Current.Name, statusPattern);
                        if (match.Success)
                        {
                            _logger.LogInformation("Presence element name found: {ElementCurrentName}", element.Current.Name);

                            // Let's grab the status by looking at everything after "displayed as ", removing the trailing ".",
                            // and setting it to lowercase. I set it to lowercase because that is how I have my ESP32-C3
                            // set up to read the data that this C# app sends to it.
                            presenceStatus = match.Groups[1].Value.Trim();
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WindowsAutomation - Error reading status");
            }

            _logger.LogDebug("WindowsAutomation status found: {PresenceStatus}", presenceStatus);

            var newStatus = _lastStatus;

            // Return what we found
            switch (presenceStatus)
            {
                case "Available":
                    newStatus = MicrosoftTeamsStatus.Available;
                    break;

                case "Busy":
                    newStatus = MicrosoftTeamsStatus.Busy;
                    break;

                case "Presenting":
                case "Do not disturb":
                    newStatus = MicrosoftTeamsStatus.DoNotDisturb;
                    break;

                default:
                    _logger.LogWarning("WindowsAutomation availability unknown: {PresenceStatus}", presenceStatus);
                    newStatus = MicrosoftTeamsStatus.Unknown;
                    break;
            }

            if (newStatus != _lastStatus)
            {
                _lastStatus = newStatus;
                _logger.LogInformation("WindowsAutomation status set to {LastStatus}", _lastStatus);
            }

            return _lastStatus;
        }
    }
}