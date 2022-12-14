using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;
using System.Text;

namespace OnlineStatusLight.Application
{
    public class MicrosoftTeamsService : IMicrosoftTeamsService
    {
        private readonly ILogger<MicrosoftTeamsService> _logger;
        private MicrosoftTeamsStatus _lastStatus = MicrosoftTeamsStatus.Unknown;
        private DateTime _fileLastUpdated = DateTime.MinValue;

        public int PoolingInterval { get; set; }
        public string LogsFilePath { get; set; }

        public MicrosoftTeamsService(IConfiguration configuration, ILogger<MicrosoftTeamsService> logger)
        {
            _logger = logger;

            this.PoolingInterval = Convert.ToInt32(configuration["msteams:interval"]);
            this.LogsFilePath = Environment.ExpandEnvironmentVariables(Convert.ToString(configuration["msteams:logfile"]));
        }

        public MicrosoftTeamsStatus GetCurrentStatus()
        {
            var fileInfo = new FileInfo(this.LogsFilePath);
            if (fileInfo.Exists)
            {
                var fileLastUpdated = fileInfo.LastWriteTime;
                if (fileLastUpdated > _fileLastUpdated)
                {
                    var lines = ReadLines(this.LogsFilePath);
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

        private IEnumerable<string> ReadLines(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}