using System.Diagnostics.CodeAnalysis;

namespace ConversationalSearchPlatform.BackOffice.Exceptions;

internal static class ThrowHelper
{
    
    [DoesNotReturn]
    internal static void ThrowConversationNotFoundException(Guid conversationId) =>
        throw new ConversationNotFoundException($"The conversation with id {conversationId} can not be found");

    
    [DoesNotReturn]
    internal static void ThrowTenantNotFoundException(string tenantId) =>
        throw new ConversationNotFoundException($"The tenant with id {tenantId} can not be found");

}