using System.Collections.Concurrent;
using ConversationalSearchPlatform.BackOffice.Data;
using Microsoft.AspNetCore.Identity;

namespace ConversationalSearchPlatform.BackOffice.Middleware;

public class LoginInfo(string email, string? password, bool rememberMe)
{
    public string UserName { get; set; } = email;

    public string? Password { get; set; } = password;

    public bool RememberMe { get; set; } = rememberMe;
}

/// <summary>
/// Temporarily contains log in requests until redirects to this middleware happen.
/// </summary>
public class BlazorCookieLoginMiddleware
{
    public static IDictionary<Guid, LoginInfo> Logins { get; private set; } = new ConcurrentDictionary<Guid, LoginInfo>();


    private readonly RequestDelegate _next;

    public BlazorCookieLoginMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, SignInManager<ApplicationUser> signInMgr)
    {
        if (context.Request.Path == "/login" && context.Request.Query.ContainsKey("key"))
        {
            await HandleLoginAsync(context, signInMgr);
        }
        else if (context.Request.Path == "/logout")
        {
            await HandleLogOutAsync(context, signInMgr);
        }
        else
        {
            await _next.Invoke(context);
        }
    }

    private static async Task HandleLogOutAsync(HttpContext context, SignInManager<ApplicationUser> signInMgr)
    {
        await signInMgr.SignOutAsync();
        context.Response.Redirect("/");
    }

    private static async Task HandleLoginAsync(HttpContext context, SignInManager<ApplicationUser> signInMgr)
    {
        var unparsedKey = context.Request.Query["key"];
        var key = Guid.Parse(unparsedKey!);
        var info = Logins[key];

        var result = await signInMgr.PasswordSignInAsync(info.UserName, info.Password!, info.RememberMe, lockoutOnFailure: true);
        info.Password = null;

        if (result.Succeeded)
        {
            Logins.Remove(key);
            context.Response.Redirect("/");
        }
        else if (result.RequiresTwoFactor)
        {
            context.Response.Redirect("/loginwith2fa/" + key);
        }
        else
        {
            context.Response.Redirect("/Account/Login");
        }
    }
}