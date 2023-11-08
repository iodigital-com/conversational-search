using ConversationalSearchPlatform.BackOffice.Constants;
using Microsoft.AspNetCore.Components.Authorization;

namespace ConversationalSearchPlatform.BackOffice.Components.Layout.Helper;

public static class DemoHelper
{

    public static bool ShouldNavigateToDemoPage(bool shouldShowNavMenu, string currentPath) =>
        !shouldShowNavMenu && !string.Equals(currentPath, "demo", StringComparison.InvariantCultureIgnoreCase);

    public static bool DetermineNavbarVisible(AuthenticationState authState)
    {
        var hasUser = authState.User.Identity != null;

        var result = true;

        if (!hasUser)
        {
            return result;
        }

        var identity = authState.User.Identity!;

        if (!identity.IsAuthenticated)
        {
            return result;
        }

        var isReadonlyUser = authState.User.IsInRole(RoleConstants.Readonly);

        if (isReadonlyUser)
        {
            result = false;
        }

        return result;
    }
}