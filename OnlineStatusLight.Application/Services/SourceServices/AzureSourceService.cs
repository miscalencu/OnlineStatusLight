using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using OnlineStatusLight.Application.Authentication;
using OnlineStatusLight.Core.Configuration;
using OnlineStatusLight.Core.Exceptions;
using OnlineStatusLight.Core.Models;
using OnlineStatusLight.Core.Services;

namespace OnlineStatusLight.Application.Services.SourceServices
{
    public class AzureSourceService : IMicrosoftTeamsService
    {
        private readonly SourceAzureConfiguration _azureConfiguration;
        private readonly ILogger<AzureSourceService> _logger;

        // set the scope for API call to user.read
        private string[] scopes = new string[] { "Presence.Read" };

        private MicrosoftTeamsStatus _lastStatus = MicrosoftTeamsStatus.Unknown;

        private bool waitingAuthentication;
        private static AuthenticationResult? authResult;

        public AzureSourceService(
            IOptions<SourceAzureConfiguration> azureConfiguration,
            ILogger<AzureSourceService> logger)
        {
            if (azureConfiguration == null)
                throw new ConfigurationException("Configuration not found for source service Azure.");

            _azureConfiguration = azureConfiguration.Value;
            PoolingInterval = _azureConfiguration.Interval;
            _logger = logger;
        }

        public int PoolingInterval { get; set; }

        public async Task<MicrosoftTeamsStatus> GetCurrentStatus()
        {
            await Authenticate();
            var availability = await GetAvailability();

            return availability;
        }

        // TODO: move to a separate Authentication service
        private async Task<AuthenticationResult?> Authenticate()
        {
            if (authResult != null)
                return authResult;

            if (waitingAuthentication)
                return default;

            waitingAuthentication = true;

            // Initialize the MSAL library by building a public client application
            var app = PublicClientApplicationBuilder.Create(_azureConfiguration.ClientId)
                .WithAuthority($"{_azureConfiguration.Authority}/{_azureConfiguration.TenantId}")
                .WithExtraQueryParameters($"client_secret={_azureConfiguration.ClientSecret}")
                .WithRedirectUri(_azureConfiguration.RedirectUri)
                //this is the currently recommended way to log MSAL message. For more info refer to https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/logging
                .WithLogging(new IdentityLogger(EventLogLevel.Warning), enablePiiLogging: false) //set Identity Logging level to Warning which is a middle ground
                .Build();

            AuthenticationResult result;
            try
            {
                var accounts = await app.GetAccountsAsync();
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
                _logger.LogInformation($"AzureAD silent authentication result: {result.Account.Username}");
            }
            catch (Exception)
            {
                result = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
                _logger.LogInformation($"AzureAD interactive authentication result: {result.Account.Username}");
            }

            authResult = result;
            waitingAuthentication = false;

            return result;
        }

        private async Task<MicrosoftTeamsStatus> GetAvailability()
        {
            if (authResult == null)
                return default;

            try
            {
                var authenticationProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(authResult.AccessToken));

                var graphClient = new GraphServiceClient(authenticationProvider);
                if (graphClient == null)
                    throw new Exception("Cannot initialize GraphServiceClient");

                // Call the /me endpoint of Graph
                var presenceRequestBuilder = graphClient.Me.Presence;
                var presence = await presenceRequestBuilder.GetAsync();

                if (presence == null)
                    throw new Exception("Cannot get presence from the GraphServiceClient");

                var availability = presence.Availability;
                var newStatus = _lastStatus;

                switch (availability)
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
                        _logger.LogWarning($"MSGraph availability unknown: {availability}");
                        newStatus = MicrosoftTeamsStatus.Unknown;
                        break;
                }

                if (newStatus != _lastStatus)
                {
                    _lastStatus = newStatus;
                    _logger.LogInformation($"MS Teams status set to {_lastStatus}");
                }
            }
            catch (MsalException msalEx)
            {
                authResult = null;
                _logger.LogError($"Error Acquiring Token:{Environment.NewLine}{msalEx}");
            }
            catch (Exception ex)
            {
                authResult = null;
                _logger.LogError($"Error Acquiring Token Silently:{Environment.NewLine}{ex}");
            }

            return _lastStatus;
        }
    }
}