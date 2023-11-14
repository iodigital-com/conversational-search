using System.Diagnostics.CodeAnalysis;
using ConversationalSearchPlatform.BackOffice.Data.Entities;

namespace ConversationalSearchPlatform.BackOffice.Exceptions;

internal static class ThrowHelper
{

    [DoesNotReturn]
    internal static void ThrowConversationNotFoundException(Guid conversationId) =>
        throw new ConversationNotFoundException($"The conversation with id {conversationId} can not be found");


    [DoesNotReturn]
    internal static void ThrowTenantNotFoundException(string tenantId) =>
        throw new ConversationNotFoundException($"The tenant with id {tenantId} can not be found");

    [DoesNotReturn]
    internal static void ThrowWebsitePageNotFoundException(Guid id) =>
        throw new WebsitePageNotFoundException($"The {nameof(WebsitePage)} with id {id} can not be found");

    [DoesNotReturn]
    internal static void ThrowInvalidWebsitePageUrl(Guid id) =>
        throw new InvalidWebsitePageUrlException($"The url of the {nameof(WebsitePage)} with id {id} is invalid");

}