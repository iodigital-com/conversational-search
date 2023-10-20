using ConversationalSearchPlatform.BackOffice.Api.Indexing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ConversationalSearchPlatform.BackOffice;

using BackOffice;
using Api;
using Api.Conversation;
using Bootstrap;
using Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Constants;
using Data;
using Identity;
using Middleware;
using Tenants;
using MudBlazor.Services;
using Serilog;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog((_, configuration) => configuration.WriteTo.Console());
        builder.Services.AddDevelopmentHttpLogging();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<UserAccessor>();
        builder.Services.AddScoped<IdentityRedirectManager>();
        builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
        builder.Services.AddMudServices();

        builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(nameof(TenantApiKeyHeaderRequirement),
                policy =>
                    policy.Requirements.Add(new TenantApiKeyHeaderRequirement()));
        });

        builder.Services.AddScoped<IAuthorizationHandler, TenantApiKeyHeaderHandler>();

        builder.Services.AddMemoryCache();
        builder.Services.AddDistributedMemoryCache();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        Action<DbContextOptionsBuilder> dbCustomization = options =>
        {
#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
            options.UseSqlServer(connectionString);
        };

        builder.Services.AddDbContextFactory<ApplicationDbContext>(dbCustomization);
        builder.Services.AddDbContextFactory<TenantDbContext>(dbCustomization);

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();


        builder.Services.AddMultiTenant<ApplicationTenantInfo>()
            .WithDelegateStrategy(Tenancy.ByUserStrategy)
            .WithEFCoreFactoryCreatingStore<TenantDbContext, ApplicationTenantInfo>();

        builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();


        builder.Services.AddSingleton<IEmailSender, NoOpEmailSender>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHangfire(builder.Configuration);
        builder.Services.AddOpenAi(builder.Configuration);
        builder.Services.AddIndexingServices();
        builder.Services.AddConversationServices();
        builder.Services.AddUserServices();
        builder.Services.AddJobServices(builder.Configuration);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();
        builder.Services.AddSwagger();
        builder.Services.AddCors(options => options.AddDefaultPolicy(
            x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials())
        );
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseHttpsRedirection();
        app.UseMiddleware<BlazorCookieLoginMiddleware>();
        // app.UseMiddleware<ApiKeyMiddleware>();
        app.UseMultiTenant();
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapGroup(ApiConstants.ApiV1Path)
            .MapConversationalSearchGroup()
            .MapIndexingGroup();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.UseSwaggerWithUi();
        app.UseHangfireDashboard(builder.Configuration);
// app.MapAdditionalIdentityEndpoints();

        app.Run();
    }
}