using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;
using System.Text;

namespace OnlineStatusLight.Application
{
    public class MicrosoftTeamsService : IMicrosoftTeamsService
    {
        private readonly ILogger<SonoffBasicR3Service> _logger;
        private MicrosoftTeamsStatus _lastStatus = MicrosoftTeamsStatus.Unknown;
        private DateTime _fileLastUpdated = DateTime.MinValue;

        public int PoolingInterval { get; set; }
        public string LogsFilePath { get; set; }

        public MicrosoftTeamsService(IConfiguration configuration, ILogger<SonoffBasicR3Service> logger)
        {
            _logger = logger;

            this.PoolingInterval = Convert.ToInt32(configuration["msteams:interval"]);
            this.LogsFilePath = Convert.ToString(configuration["msteams:logfile"]);
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
                                switch (status)
                                {
                                    case "Available":
                                        _lastStatus = MicrosoftTeamsStatus.Available;
                                        break;
                                    case "Away":
                                        _lastStatus = MicrosoftTeamsStatus.Away;
                                        break;
                                    case "Busy":
                                    case "OnThePhone":
                                        _lastStatus = MicrosoftTeamsStatus.Busy;
                                        break;
                                    case "DoNotDisturb":
                                        _lastStatus = MicrosoftTeamsStatus.DoNotDisturb;
                                        break;
                                    case "BeRightBack":
                                        _lastStatus = MicrosoftTeamsStatus.Away;
                                        break;
                                    case "Offline":
                                        _lastStatus = MicrosoftTeamsStatus.Offline;
                                        break;
                                    default:
                                        _logger.LogWarning($"MS Teams status unknown: {status}");
                                        _lastStatus = MicrosoftTeamsStatus.Unknown;
                                        break;
                                }

                                _logger.LogInformation($"MS Teams status set to {_lastStatus}");
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