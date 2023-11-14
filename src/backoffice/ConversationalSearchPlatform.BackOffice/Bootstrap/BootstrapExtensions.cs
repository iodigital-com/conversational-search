using System.Net.Http.Headers;
using System.Reflection;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Jobs;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Implementations;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.Weaviate;
using ConversationalSearchPlatform.BackOffice.Swagger;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Stores;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Hangfire;
using Hangfire.Console;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Rystem.OpenAi;
using Swashbuckle.AspNetCore.Filters;
using TurnerSoftware.SitemapTools;
using ReferenceType = Microsoft.OpenApi.Models.ReferenceType;

namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

internal static class BootstrapExtensions
{
    internal static IServiceCollection AddDevelopmentHttpLogging(this IServiceCollection services)
    {
#if DEBUG
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
                                    HttpLoggingFields.RequestBody |
                                    HttpLoggingFields.ResponseBody;
        });
#endif
        return services;
    }

    internal static IEndpointConventionBuilder MapHealthCheck(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapHealthChecks("/health");

    internal static IServiceCollection AddIndexingServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IIndexingService<WebsitePage>, WebsitePageIndexingService>();
        return serviceCollection;
    }

    internal static IServiceCollection AddJobServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        // serviceCollection.AddTransient<IScraperService, SimpleScraperService>();
        // serviceCollection.AddTransient<IScraperService, PuppeteerScraperService>();
        // serviceCollection.AddHttpClient<IScraperService, SimpleScraperService>();
        serviceCollection.AddTransient<IScraperService, PuppeteerScraperService>();

        var puppeteerSection = configuration.GetSection("Puppeteer");
        serviceCollection.AddOptions<PuppeteerSettings>().Bind(puppeteerSection);
        var puppeteerSettings = puppeteerSection.Get<PuppeteerSettings>() ?? throw new InvalidOperationException("No Puppeteersettings found");

        serviceCollection.AddHttpClient<IScraperService, PuppeteerScraperService>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(puppeteerSettings.BaseUrl);
            });

        serviceCollection
            .AddTransient<IWeaviateRecordCreator<ChunkResult, ChunkCollection, WebsitePageWeaviateCreateRecord>,
                WebsitePageRecordCreator<ChunkResult, ChunkCollection, WebsitePageWeaviateCreateRecord>>();
        serviceCollection
            .AddTransient<IWeaviateRecordCreator<ImageResult, ImageCollection, ImageWeaviateCreateRecord>,
                ImageRecordCreator<ImageResult, ImageCollection, ImageWeaviateCreateRecord>>();
        serviceCollection.AddTransient<IChunkService, UnstructuredChunkService>();
        serviceCollection.AddTransient<IVectorizationService, WeaviateVectorizationService>();


        var unstructuredSection = configuration.GetSection("Unstructured");
        serviceCollection.AddOptions<UnstructuredSettings>().Bind(unstructuredSection);

        serviceCollection.AddHttpClient<IChunkService, UnstructuredChunkService>()
            .ConfigureHttpClient((sp, client) =>
            {
                var unstructuredUrl = sp.GetRequiredService<IOptions<UnstructuredSettings>>().Value;
                client.BaseAddress = new Uri(unstructuredUrl.BaseUrl);
            });

        var configurationSection = configuration.GetSection("Weaviate");
        serviceCollection.AddOptions<WeaviateSettings>().Bind(configurationSection);

        serviceCollection.AddHttpClient("Weaviate",
            (sp, client) =>
            {
                var weaviateSettings = sp.GetRequiredService<IOptions<WeaviateSettings>>().Value ?? throw new InvalidOperationException($"No {nameof(WeaviateSettings)} found");

                client.BaseAddress = new Uri(weaviateSettings.BaseUrl);

                if (!string.IsNullOrWhiteSpace(weaviateSettings.ApiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", weaviateSettings.ApiKey);
                }
            });

        var graphQlWebsocketJsonSerializer = new SystemTextJsonSerializer();

        serviceCollection.AddScoped<IGraphQLClient>(sp =>
        {
            var weaviateSettings = sp.GetRequiredService<IOptions<WeaviateSettings>>().Value ?? throw new InvalidOperationException($"No {nameof(WeaviateSettings)} found");

            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("Weaviate");

            return new GraphQLHttpClient(
                new GraphQLHttpClientOptions
                {
                    EndPoint = new Uri($"{weaviateSettings.BaseUrl}/v1/graphql"),
                },
                graphQlWebsocketJsonSerializer,
                httpClient
            );
        });

        return serviceCollection;
    }

    internal static IServiceCollection AddConversationServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IConversationService, ConversationService>();
        return serviceCollection;
    }

    internal static IServiceCollection AddStatisticsServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IStatisticsService, StatisticsService>();
        return serviceCollection;
    }

    internal static IServiceCollection AddSitemapServices(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var sitemapStorageSection = configuration.GetSection("SitemapStorage");
        serviceCollection.AddOptions<SitemapStorageSettings>().Bind(sitemapStorageSection);
        _ = sitemapStorageSection.Get<SitemapStorageSettings>() ?? throw new InvalidOperationException("No Puppeteersettings found");

        serviceCollection.AddTransient<IAzureBlobStorage, SitemapBlobStorageService>();
        serviceCollection.AddTransient<SitemapQuery>();
        serviceCollection.AddTransient<ISitemapParsingService, SitemapParsingService>();
        serviceCollection.AddHostedService<AzureRegisterStorageContainerJob>();
        return serviceCollection;
    }

    internal static IServiceCollection AddUserServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IUserInviteService, UserInviteService>();
        return serviceCollection;
    }

    // ReSharper disable once InconsistentNaming
    internal static FinbuckleMultiTenantBuilder<TTenantInfo> WithEFCoreFactoryCreatingStore<TEFCoreStoreDbContext, TTenantInfo>(
        this FinbuckleMultiTenantBuilder<TTenantInfo> builder)
        where TEFCoreStoreDbContext : EFCoreStoreDbContext<TTenantInfo>
        where TTenantInfo : class, ITenantInfo, new()
    {
        builder.Services.AddDbContext<TEFCoreStoreDbContext>();
        return builder.WithStore<EFCoreFactoryCreatingStore<TEFCoreStoreDbContext, TTenantInfo>>(ServiceLifetime.Scoped);
    }

    internal static IServiceCollection AddOpenAITelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var azurePricingSection = configuration.GetSection("AzurePricing");
        services.AddOptions<AzurePricingSettings>().Bind(azurePricingSection);

        services.AddTransient<IOpenAIPriceFetchingService, OpenAIPriceFetchingService>();

        services.AddHttpClient<IOpenAIPriceFetchingService, OpenAIPriceFetchingService>((provider, client) =>
        {
            var azurePricingSettings = provider.GetRequiredService<IOptions<AzurePricingSettings>>().Value ??
                                       throw new InvalidOperationException($"No {nameof(AzurePricingSettings)} found");
            client.BaseAddress = new Uri(azurePricingSettings.BaseUrl);
        });

        services.AddTransient<IOpenAIPricingService, OpenAIPricingService>();
        services.AddTransient<IOpenAIUsageTelemetryService, OpenAIUsageTelemetryService>();
        return services;
    }


    internal static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        var securityScheme = CreateApiKeyHeaderSecurityScheme();
        var securityRequirement = CreateApiKeyHeaderSecurityRequirement();

        var contact = new OpenApiContact
        {
            Url = new Uri("https://iodigital.com/")
        };

        var info = new OpenApiInfo
        {
            Version = "v1",
            Title = "Polestar POC API",
            Contact = contact
        };

        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", info);
            o.AddSecurityDefinition("ApiKey", securityScheme);
            o.AddSecurityRequirement(securityRequirement);
            var filePath = Path.Combine(AppContext.BaseDirectory, "ConversationalSearchPlatform.BackOffice.xml");
            o.IncludeXmlComments(filePath);
            o.SchemaFilter<EnumSchemaFilter>();
            o.OperationFilter<AuthenticationRequirementsOperationFilter>();
            o.EnableAnnotations();
            o.ExampleFilters();
        });
        services.AddSwaggerExamplesFromAssemblies(Assembly.GetEntryAssembly());

        return services;
    }

    internal static IApplicationBuilder UseSwaggerWithUi(this IApplicationBuilder application)
    {
        application.UseSwagger();
        application.UseSwaggerUI(options =>
        {
            options.EnableTryItOutByDefault();
            options.InjectStylesheet("/swagger-ui/SwaggerDark.css");
        });
        return application;
    }

    internal static IServiceCollection AddOpenAi(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetSection("OpenAI");
        var openAiSettings = configurationSection.Get<OpenAISettings>() ?? throw new InvalidOperationException("No OpenAiSettings found");
        services.AddOptions<OpenAISettings>().Bind(configurationSection);

        services.AddOpenAi(settings =>
        {
            settings.ApiKey = openAiSettings.ApiKey;

            if (openAiSettings.UseAzure)
            {
                settings.Azure.ResourceName = openAiSettings.ResourceName;
                settings.UseVersionForChat(openAiSettings.VersionForChat);
                settings.Azure.MapDeploymentChatModel("gpt-4-io-gpt", ChatModelType.Gpt4);
                settings.Azure.MapDeploymentChatModel("gpt-4-large-io-gpt", ChatModelType.Gpt4_32K);
                settings.Azure.MapDeploymentChatModel("gpt-35-turbo-io-gpt", ChatModelType.Gpt35Turbo);
                settings.Azure.MapDeploymentEmbeddingModel("text-embedding-ada-002-io-gpt", EmbeddingModelType.AdaTextEmbedding);
            }
        });
        return services;
    }

    internal static IServiceCollection AddHangfire(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseFilter(new LogJobFilter())
            .UseConsole()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));
        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 5; // TODO: might need to change this later. This is to not choke the parser for now
            options.Queues = QueueConstants.Queues;
        });

        return services;
    }

    internal static IServiceCollection AddJobScheduler(this IServiceCollection services)
    {
        services.AddHostedService<RecurringJobScheduler>();
        return services;
    }

    public static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app, IConfiguration config)
    {
        var dashboardOptions = config.GetRequiredSection("Hangfire:Dashboard").Get<DashboardOptions>() ?? throw new InvalidOperationException("");

// #if !DEBUG
//               dashboardOptions.Authorization = new[]
//         {
//             new HangfireCustomBasicAuthenticationFilter
//             {
//                 User = config.GetSection("Hangfire:Credentials:User").Value,
//                 Pass = config.GetSection("Hangfire:Credentials:Password").Value
//             }
//         };
// #endif
        return app.UseHangfireDashboard(config["Hangfire:Route"], dashboardOptions);
    }

    private static OpenApiSecurityRequirement CreateApiKeyHeaderSecurityRequirement() => new()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Header"
                }
            },
            Array.Empty<string>()
        }
    };

    private static OpenApiSecurityScheme CreateApiKeyHeaderSecurityScheme() => new()
    {
        Name = HeaderConstants.TenantHeader,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Header",
        In = ParameterLocation.Header,
        Description = $"Api Key",
    };

}