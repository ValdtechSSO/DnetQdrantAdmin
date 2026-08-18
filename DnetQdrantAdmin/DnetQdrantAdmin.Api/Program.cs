using Dnet.QdrantAdmin.Api.Apis;
using Dnet.QdrantAdmin.Api.Infrastructure.Embeddings;
using Dnet.QdrantAdmin.Api.Infrastructure.Factories;
using Dnet.QdrantAdmin.Api.Infrastructure.Middleware;
using Dnet.QdrantAdmin.Api.Infrastructure.Models;
using Dnet.QdrantAdmin.Api.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Qdrant gRPC runs over unencrypted HTTP/2 (h2c).
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

builder.Services.Configure<LlmProviderConfig>(builder.Configuration.GetSection("LlmProviderConfig"));

builder.Services.Configure<QdrantConfig>(builder.Configuration.GetSection("QdrantConfig"));

var httpClientOptions = builder.Configuration.GetSection("HttpClientOptions").Get<HttpClientOptions>() ?? new HttpClientOptions();

RegisterEmbeddingProviders(builder.Services, builder.Configuration.GetSection("LlmProviderConfig").Get<LlmProviderConfig>() ?? new LlmProviderConfig(), httpClientOptions);

builder.Services.AddCors(
               options =>
               {
                   var origins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
                       ?? ["https://localhost:7188"];

                   options.AddPolicy("CorsPolicy",
                       builder => builder
                           .WithOrigins(origins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .Build());
               });

builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();

builder.Services.AddTransient<IProblemDetailFactory, ProblemDetailFactory>();

builder.Services.AddScoped<IQdrantService, QdrantService>();

builder.Services.AddSingleton<QdrantRestClient>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCors("CorsPolicy");

app.UseMiddleware<ExceptionHandlingMiddleware>();

var qdrant = app.MapGroup("/api/Qdrant");
qdrant.QdrantApis();

var llmProviders = app.MapGroup("/api/LlmProviders");
llmProviders.LlmProviderApis();

app.MapGet("/healthz", () => Results.Ok("healthy"));

app.Run();

static void RegisterEmbeddingProviders(IServiceCollection services, LlmProviderConfig config, HttpClientOptions httpClientOptions)
{
    foreach (var providerConfig in config.Providers)
    {
        switch (providerConfig.Type.ToLowerInvariant())
        {
            case "openai":
                RegisterOpenAIProvider(services, providerConfig, httpClientOptions);
                break;

            case "azureopenai":
            case "azure":
                RegisterAzureOpenAIProvider(services, providerConfig, httpClientOptions);
                break;

            case "ollama":
                services.AddSingleton<IEmbeddingProvider>(new OllamaEmbeddingProvider(providerConfig));
                break;

            case "fastembed":
                services.AddSingleton<IEmbeddingProvider>(new FastEmbedProvider(providerConfig));
                break;

            default:
                throw new InvalidOperationException($"Embedding provider type '{providerConfig.Type}' is not supported (provider '{providerConfig.Name}')");
        }
    }
}

static void RegisterOpenAIProvider(IServiceCollection services, EmbeddingProviderConfig providerConfig, HttpClientOptions httpClientOptions)
{
    if (string.IsNullOrWhiteSpace(providerConfig.ApiKey))
    {
        return;
    }

    services.AddSingleton<IEmbeddingProvider>(sp => new OpenAIEmbeddingProvider(providerConfig, sp));

    var httpClient = httpClientOptions.IgnoreCertificateRevocationErrors ? CreateLenientHttpClient() : null;

    foreach (var model in providerConfig.Models)
    {
        foreach (var dimension in model.Dimensions)
        {
#pragma warning disable SKEXP0010 // AddOpenAIEmbeddingGenerator is experimental
            services.AddOpenAIEmbeddingGenerator(
                modelId: model.Model,
                apiKey: providerConfig.ApiKey,
                orgId: null,
                dimensions: model.Model == "text-embedding-ada-002" ? null : dimension,
                serviceId: EmbeddingService.GeneratorServiceId(providerConfig.Name, model.Model, dimension),
                httpClient: httpClient);
#pragma warning restore SKEXP0010 // AddOpenAIEmbeddingGenerator is experimental
        }
    }
}

static void RegisterAzureOpenAIProvider(IServiceCollection services, EmbeddingProviderConfig providerConfig, HttpClientOptions httpClientOptions)
{
    if (string.IsNullOrWhiteSpace(providerConfig.ApiKey) || string.IsNullOrWhiteSpace(providerConfig.Endpoint))
    {
        return;
    }

    services.AddSingleton<IEmbeddingProvider>(sp => new AzureOpenAIEmbeddingProvider(providerConfig, sp));

    var httpClient = httpClientOptions.IgnoreCertificateRevocationErrors ? CreateLenientHttpClient() : null;

    foreach (var model in providerConfig.Models)
    {
        foreach (var dimension in model.Dimensions)
        {
#pragma warning disable SKEXP0010 // AddAzureOpenAIEmbeddingGenerator is experimental
            services.AddAzureOpenAIEmbeddingGenerator(
                deploymentName: model.Model,
                endpoint: providerConfig.Endpoint,
                apiKey: providerConfig.ApiKey,
                serviceId: EmbeddingService.GeneratorServiceId(providerConfig.Name, model.Model, dimension),
                modelId: model.Model,
                dimensions: dimension,
                httpClient: httpClient);
#pragma warning restore SKEXP0010 // AddAzureOpenAIEmbeddingGenerator is experimental
        }
    }
}

/// <summary>
/// Creates an HttpClient that still validates certificate trust but tolerates an
/// unknown/offline revocation status. Some networks cannot reach the OCSP responder
/// used by api.openai.com, which makes .NET fail the handshake with
/// "RevocationStatusUnknown" while curl/browsers succeed.
/// </summary>
static HttpClient CreateLenientHttpClient()
{
    var handler = new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = ValidateCertificateIgnoringRevocationStatus
        }
    };

    return new HttpClient(handler);
}

static bool ValidateCertificateIgnoringRevocationStatus(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
{
    if (sslPolicyErrors == SslPolicyErrors.None)
    {
        return true;
    }

    // Accept the certificate when the only problem is that the revocation status
    // could not be determined (OCSP/CRL unreachable): trust itself is still validated.
    if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && chain is not null)
    {
        var onlyRevocationUnknown = chain.ChainStatus
            .Select(s => s.Status)
            .All(s => s is X509ChainStatusFlags.RevocationStatusUnknown or X509ChainStatusFlags.OfflineRevocation);

        if (onlyRevocationUnknown)
        {
            return true;
        }
    }

    return false;
}
