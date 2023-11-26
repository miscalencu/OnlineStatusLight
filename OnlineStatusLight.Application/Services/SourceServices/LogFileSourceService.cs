using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineStatusLight.Core.Configuration;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;
using OnlineStatusLight.Core.Exceptions;
using System.Text;

namespace OnlineStatusLight.Application.Services.SourceServices
{
    public class LogFileSourceService : IMicrosoftTeamsService
    {
        private readonly ILogger<IMicrosoftTeamsService> _logger;
        private MicrosoftTeamsStatus _lastStatus = MicrosoftTeamsStatus.Unknown;
        private DateTime _fileLastUpdated = DateTime.MinValue;

        public int PoolingInterval { get; set; }
        public string LogsFilePath { get; set; }

        public LogFileSourceService(
            IOptions<SourceLogFileConfiguration> logFileConfigurationOptions,
            ILogger<IMicrosoftTeamsService> logger)
        {
            _logger = logger;

            if (logFileConfigurationOptions == null)
                throw new ConfigurationException("Configuration not found for source service LogFile.");

            if (logFileConfigurationOptions.Value.Interval == default)
                throw new ConfigurationException("Interval is not set in configuration for source LogFile (0 is not allowed).");

            if (logFileConfigurationOptions.Value.Path == null)
                throw new ConfigurationException("Path is not set in configuration for source LogFile.");

            PoolingInterval = logFileConfigurationOptions.Value.Interval;
            LogsFilePath = Environment.ExpandEnvironmentVariables(logFileConfigurationOptions.Value.Path);
        }

        public async Task<MicrosoftTeamsStatus> GetCurrentStatus()
        {
            var fileInfo = new FileInfo(LogsFilePath);
            if (fileInfo.Exists)
            {
                var fileLastUpdated = fileInfo.LastWriteTime;
                if (fileLastUpdated > _fileLastUpdated)
                {
                    var lines = await ReadLines(LogsFilePath);
                    foreach (var line in lines.Reverse())
                    {
                        var delFrom = " (current state: ";
                        var delTo = ") ";

                        if (line.Contains(delFrom) && line.Contains(delTo))
                        {
                            int posFrom = line.IndexOf(delFrom) + delFrom.Length;
                            int posTo = line.IndexOf(delTo, posFrom);

                            if (posFrom <= posTo)
                            {
                                var info = line.Substring(posFrom, posTo - posFrom);
                                var status = info.Split(" -> ").Last();
                                var newStatus = _lastStatus;

                                switch (status)
                                {
                                    case "Available":
                                        newStatus = MicrosoftTeamsStatus.Available;
                                        break;

                                    case "Away":
                                        newStatus = MicrosoftTeamsStatus.Away;
                                        break;

                                    case "Busy":
                                    case "OnThePhone":
                                        newStatus = MicrosoftTeamsStatus.Busy;
                                        break;

                                    case "DoNotDisturb":
                                    case "Presenting":
                                        newStatus = MicrosoftTeamsStatus.DoNotDisturb;
                                        break;

                                    case "BeRightBack":
                                        newStatus = MicrosoftTeamsStatus.Away;
                                        break;

                                    case "Offline":
                                        newStatus = MicrosoftTeamsStatus.Offline;
                                        break;

                                    case "NewActivity":
                                        // ignore this - happens where there is a new activity: Message, Like/Action, File Upload
                                        // this is not a real status change, just shows the bell in the icon
                                        break;

                                    case "InAMeeting":
                                        newStatus = MicrosoftTeamsStatus.InAMeeting;
                                        break;

                                    default:
                                        _logger.LogWarning($"MS Teams status unknown: {status}");
                                        newStatus = MicrosoftTeamsStatus.Unknown;
                                        break;
                                }

                                if (newStatus != _lastStatus)
                                {
                                    _lastStatus = newStatus;
                                    _logger.LogInformation($"MS Teams status set to {_lastStatus}");
                                }
                                break;
                            }
                        }
                    }
                    _fileLastUpdated = fileLastUpdated;
                }
            }

            // TO DO
            return _lastStatus;
        }

        private async Task<IEnumerable<string>> ReadLines(string path)
        {
            var lines = new List<string>();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = await sr.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }
    }
}