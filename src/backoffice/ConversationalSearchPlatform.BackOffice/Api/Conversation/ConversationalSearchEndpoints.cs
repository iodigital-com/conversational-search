using ConversationalSearchPlatform.BackOffice.Api.Extensions;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Mvc;

namespace ConversationalSearchPlatform.BackOffice.Api.Conversation;

public static class ConversationalSearchEndpoints
{
    private const string ConversationalSearchTag = "Conversational Search";

    public static IEndpointRouteBuilder MapConversationalSearchGroup(this IEndpointRouteBuilder outerGroup)
    {
        var innerGroup = outerGroup.MapGroup(string.Empty)
            .RequireAuthorization(nameof(TenantApiKeyHeaderRequirement))
            .WithTags(ConversationalSearchTag);

        innerGroup.MapPost($"/conversation",
            async (
                HttpContext httpContext,
                [FromServices] IConversationService conversationService,
                [FromServices] IMultiTenantStore<ApplicationTenantInfo> tenantStore,
                [FromBody] ConversationRequest request
            ) =>
            {
                var tenantId = httpContext.GetTenantHeader();
                var tenant = await tenantStore.TryGetAsync(tenantId);

                if (tenant == null)
                {
                    ThrowHelper.ThrowTenantNotFoundException(tenantId);
                }

                var conversationId = await conversationService.StartConversationAsync(tenantId, tenant.ChatModel, tenant.AmountOfSearchReferences);
                var response = await conversationService.ConverseAsync(conversationId.Value, tenantId, request.Prompt);

                return MapToApiResponse(response);
            }).WithName("StartConversation");

        innerGroup.MapPost("/conversation/{conversationId}",
            async ([FromRoute] Guid conversationId,
                HttpContext httpContext,
                [FromServices] IConversationService conversationService,
                [FromBody] ConversationRequest request
            ) =>
            {
                var tenantId = httpContext.GetTenantHeader();
                var response = await conversationService.ConverseAsync(conversationId, tenantId, request.Prompt);

                return MapToApiResponse(response);
            }).WithName("ContinueConversation");

        return outerGroup;
    }

    private static ConversationReferencedResponse MapToApiResponse(ConversationReferencedResult response)
    {
        var conversationReferencedResponse = new ConversationReferencedResponse(
            new ConversationResponse(response.Response.ConversationId, response.Response.Answer),
            response.References.Select(reference => new ConversationReferenceResponse(reference.Index, reference.Url)).ToList()
        );
        return conversationReferencedResponse;
    }

    public record ConversationRequest(string Prompt);

    public record ConversationResponse(Guid ConversationId, string Answer);

    public record ConversationReferencedResponse(ConversationResponse Response, List<ConversationReferenceResponse> References);

    public record ConversationReferenceResponse(int Index, string Url);
}