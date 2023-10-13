using ConversationalSearchPlatform.BackOffice;
using ConversationalSearchPlatform.BackOffice.Api;
using ConversationalSearchPlatform.BackOffice.Api.Conversation;
using ConversationalSearchPlatform.BackOffice.Bootstrap;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ConversationalSearchPlatform.BackOffice.Constants;
using ConversationalSearchPlatform.BackOffice.Data;
using ConversationalSearchPlatform.BackOffice.Identity;
using ConversationalSearchPlatform.BackOffice.Middleware;
using ConversationalSearchPlatform.BackOffice.Tenants;
using MudBlazor.Services;
using Serilog;

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

builder.Services.AddDistributedMemoryCache();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
builder.Services.AddOpenAi(builder.Configuration);
builder.Services.AddIndexingServices();
builder.Services.AddUserServices();

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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<BlazorCookieLoginMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMultiTenant();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapGroup(ApiConstants.ApiV1Path).MapConversationalSearchGroup();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.UseSwaggerWithUi();
// app.MapAdditionalIdentityEndpoints();

app.Run();