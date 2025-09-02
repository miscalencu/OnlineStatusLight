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
            var windowName = _windowsAutomationConfiguration.WindowName ?? "Teams";
            var statusPattern = _windowsAutomationConfiguration.StatusPattern?.Replace("@status", "(.+)") ?? "";
            var restartProcess = _windowsAutomationConfiguration.RestartProcess;
            var restartArgument = _windowsAutomationConfiguration.RestartArgument;

            AutomationElement? teamsWindow = null;
            AutomationElement? statusButton = null;

            try
            {
                var rootElement = await Task.Run(() => AutomationElement.RootElement, cancellationToken);

                // Get all top-level windows
                var windows = await Task.Run(() =>
                {
                    var windowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
                    return rootElement.FindAll(TreeScope.Children, windowCondition);
                }, cancellationToken);

                // Find the Teams window and its status button
                foreach (AutomationElement window in windows)
                {
                    if (!window.Current.Name.Contains(windowName))
                        continue;

                    try
                    {
                        var buttons = window
                            .FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                            .Cast<AutomationElement>()
                            .ToList();

                        statusButton = buttons
                            .Where(_ => _.Current.Name != null && Regex.Match(_.Current.Name, statusPattern).Success)
                            .SingleOrDefault();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error finding presence button: {Message}", ex.Message);
                    }

                    if (statusButton != null)
                    {
                        teamsWindow = window;
                        break;
                    }
                }

                // if Teams is not running, try to restart it
                if (teamsWindow == null && restartProcess)
                {
                    var processes = Process.GetProcessesByName(_windowsAutomationConfiguration.ProcessName);
                    foreach (var process in processes)
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

                if (teamsWindow == null || statusButton == null)
                    return MicrosoftTeamsStatus.Unknown;

                var match = Regex.Match(statusButton.Current.Name, statusPattern);
                if (match.Success)
                {
                    _logger.LogInformation("Presence element name found: {ElementCurrentName}", statusButton.Current.Name);
                    presenceStatus = match.Groups[1].Value.Trim();
                }
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

                case "Away":
                    newStatus = MicrosoftTeamsStatus.Away;
                    break;

                case "Unknown":
                    newStatus = MicrosoftTeamsStatus.Unknown;
                    break;

                case "In a meeting":
                case "In a call":
                    newStatus = MicrosoftTeamsStatus.InAMeeting;
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