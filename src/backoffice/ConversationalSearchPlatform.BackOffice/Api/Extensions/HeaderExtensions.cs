using ConversationalSearchPlatform.BackOffice.Constants;

namespace ConversationalSearchPlatform.BackOffice.Api.Extensions;

public static class HeaderExtensions
{
    public static string GetTenantHeader(this HttpContext httpContext) =>
        httpContext.Request.Headers[HeaderConstants.TenantHeader][0]!;
}