using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data.Entities;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Swagger;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Stores;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using Rystem.OpenAi;

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

    internal static IServiceCollection AddIndexingServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IIndexingService<WebsitePage>, WebsitePageIndexingService>();
        return serviceCollection;
    }

    internal static IServiceCollection AddConversationServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IConversationService, ConversationService>();
        return serviceCollection;
    }

    internal static IServiceCollection AddUserServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IUserInviteService, UserInviteService>();
        return serviceCollection;
    }

    internal static FinbuckleMultiTenantBuilder<TTenantInfo> WithEFCoreFactoryCreatingStore<TEFCoreStoreDbContext, TTenantInfo>(
        this FinbuckleMultiTenantBuilder<TTenantInfo> builder)
        where TEFCoreStoreDbContext : EFCoreStoreDbContext<TTenantInfo>
        where TTenantInfo : class, ITenantInfo, new()
    {
        builder.Services.AddDbContext<TEFCoreStoreDbContext>(); // Note, will not override existing context if already added.
        return builder.WithStore<EFCoreFactoryCreatingStore<TEFCoreStoreDbContext, TTenantInfo>>(ServiceLifetime.Scoped);
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
        });

        return services;
    }

    internal static IApplicationBuilder UseSwaggerWithUi(this IApplicationBuilder application)
    {
        application.UseSwagger();
        application.UseSwaggerUI();
        return application;
    }

    internal static IServiceCollection AddOpenAi(this IServiceCollection services, IConfiguration configuration)
    {
        var configurationSection = configuration.GetSection("OpenAI");
        var openAiSettings = configurationSection.Get<OpenAISettings>() ?? throw new InvalidOperationException("No OpenAiSettings found");
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