using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using BlogComments;
using BlogComments.Functions.ModelBinding;
using BlogComments.Functions.Persistence;
using BlogComments.Functions.Validation;
using BlogComments.GitHub;
using BlogComments.GitHub.Jwt;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((builder, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddSingleton<GitHubClientFactory>();
        services.AddSingleton<ISystemClock, RealSystemClock>();
        services.AddSingleton<CommentRepository>();

        services.AddSingleton<ICryptographicSigner, KeyVaultRs256CryptographicSigner>();
        services.AddSingleton(x =>
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
            .Bind(builder.Configuration.GetSection("GitHub"))
            .Validate(opts =>
            {
                return !string.IsNullOrWhiteSpace(opts.Repository)
                    && !string.IsNullOrWhiteSpace(opts.Username);
            });

        services.AddOptions<KeyVaultOptions>()
            .Bind(builder.Configuration.GetSection("KeyVault"))
            .Validate(opts =>
            {
                return !string.IsNullOrWhiteSpace(opts.KeyName)
                    && opts.Url is not null;
            });

        services.AddTransient<ModelBinder>();
        services.AddValidatorsFromAssemblyContaining<Program>();
    })
    .Build();

host.Run();
