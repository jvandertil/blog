using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BlogComments.GitHub;
using Microsoft.Extensions.Options;
using Octokit;

namespace BlogComments
{
    public class GitHubClientFactory
    {
        private readonly AppClientTokenGenerator _tokenGenerator;
        private readonly IOptionsMonitor<GitHubOptions> _options;

        private readonly Dictionary<(string username, string repository), AccessToken> _tokenCache;
        private readonly SemaphoreSlim _cacheLock;

        public GitHubClientFactory(AppClientTokenGenerator tokenGenerator, IOptionsMonitor<GitHubOptions> options)
        {
            _tokenGenerator = tokenGenerator;
            _options = options;

            _tokenCache = new Dictionary<(string, string), AccessToken>();
            _cacheLock = new SemaphoreSlim(1, 1);
        }

        public async ValueTask<GitHubClient> CreateClientAsync(string username, string repository)
        {
            var currentToken = await GetTokenAsync(username, repository);

            return new GitHubClient(new ProductHeaderValue("jvandertil-blog-bot-inst"))
            {
                Credentials = new Credentials(currentToken.Token),
            };
        }

        private async ValueTask<AccessToken> GetTokenAsync(string username, string repository)
        {
            await _cacheLock.WaitAsync();

            try
            {
                var key = (username, repository);

                if (_tokenCache.TryGetValue(key, out var token))
                {
                    if (!TokenNeedsRefresh(token))
                    {
                        return token;
                    }
                }

                // Token needs refresh
                var newToken = await GetNewToken(username, repository);

                _tokenCache[key] = newToken;

                return newToken;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private static bool TokenNeedsRefresh([NotNullWhen(false)] AccessToken? currentToken)
        {
            if (currentToken is null)
            {
                return true;
            }

            var now = DateTimeOffset.UtcNow;
            var expirationDate = currentToken.ExpiresAt;

            var remaining = expirationDate - now;

            if (remaining.TotalMinutes < 5)
            {
                return true;
            }

            return false;
        }

        private async Task<AccessToken> GetNewToken(string username, string repository)
        {
            var settings = _options.CurrentValue;

            string appClientToken = _tokenGenerator.CreateToken(settings.ApplicationId, 30);
            var appClient = new GitHubClient(new ProductHeaderValue("jvandertil-blog-bot-app"))
            {
                Credentials = new Credentials(appClientToken, AuthenticationType.Bearer),
            };

            var installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(username, repository);
            var appToken = await appClient.GitHubApps.CreateInstallationToken(installation.Id);

            return appToken;
        }
    }
}
