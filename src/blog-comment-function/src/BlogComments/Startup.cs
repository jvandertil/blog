using System;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using BlogComments.Functions.ModelBinding;
using BlogComments.Functions.Persistence;
using BlogComments.Functions.Validation;
using BlogComments.GitHub;
using BlogComments.GitHub.Jwt;
using FluentValidation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(BlogComments.Startup))]

namespace BlogComments
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var services = builder.Services;
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddEnvironmentVariables();

            var configuration = configBuilder.Build();

            services.AddSingleton(configuration);
            services.AddSingleton<GitHubClientFactory>();
            services.AddSingleton<ISystemClock, RealSystemClock>();
            services.AddSingleton<CommentRepository>();

            services.AddSingleton<ICryptographicSigner, KeyVaultRs256CryptographicSigner>();
            services.AddSingleton<CryptographyClient>(x =>
            {
                var settingsMonitor = x.GetRequiredService<IOptionsMonitor<KeyVaultOptions>>();
                var settings = settingsMonitor.CurrentValue;

                var keyClient = new KeyClient(settings.Url, new DefaultAzureCredential());
                var key = keyClient.GetKey(settings.KeyName);

                return new CryptographyClient(key.Value.Id, new DefaultAzureCredential());
            });

            services.AddSingleton<GitHubPostExistenceValidator>();
            services.AddSingleton<IPostExistenceValidator>(x =>
            {
                var inner = x.GetRequiredService<GitHubPostExistenceValidator>();
                var decorated = new CachingPostExistenceValidatorDecorator(inner);

                return decorated;
            });

            services.AddSingleton<AppClientTokenGenerator>();

            services.AddOptions<GitHubOptions>()
                .Bind(configuration.GetSection("GitHub"))
                .Validate(opts =>
                {
                    return !string.IsNullOrWhiteSpace(opts.Repository)
                        && !string.IsNullOrWhiteSpace(opts.Username);
                });

            services.AddOptions<KeyVaultOptions>()
                .Bind(configuration.GetSection("KeyVault"))
                .Validate(opts =>
                {
                    return !string.IsNullOrWhiteSpace(opts.KeyName)
                        && !(opts.Url is null);
                });

            services.AddTransient<ModelBinder>();
            services.AddValidatorsFromAssemblyContaining<Startup>();
        }
    }

    public class KeyVaultOptions
    {
        public Uri Url { get; set; }

        public string KeyName { get; set; }

        public KeyVaultOptions()
        {
            Url = null!;
            KeyName = null!;
        }
    }

    public class GitHubOptions
    {
        public int ApplicationId { get; set; }

        public string Username { get; set; }

        public string Repository { get; set; }

        public bool EnablePullRequestCreation { get; set; } = false;

        public GitHubOptions()
        {
            Username = null!;
            Repository = null!;
        }
    }
}
