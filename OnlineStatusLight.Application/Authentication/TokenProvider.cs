using Microsoft.Kiota.Abstractions.Authentication;

namespace OnlineStatusLight.Application.Authentication
{
    public class TokenProvider : IAccessTokenProvider
    {
        private readonly string _token;

        public TokenProvider(string token) : base()
        {
            _token = token;
        }

        public AllowedHostsValidator AllowedHostsValidator => throw new NotImplementedException();

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var token = _token;
            // get the token and return it in your own way
            return Task.FromResult(token);
        }
    }
}