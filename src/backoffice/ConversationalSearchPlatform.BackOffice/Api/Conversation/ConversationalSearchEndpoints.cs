using System.Net;
using ConversationalSearchPlatform.BackOffice.Api.Conversation.Examples;
using ConversationalSearchPlatform.BackOffice.Api.Extensions;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Models.Conversations;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

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
                [SwaggerRequestExample(typeof(ConversationRequest), typeof(ConversationalSearchEndpointsExamples.SuccessExample))]
                [SwaggerResponseExample(404, typeof(ConversationalSearchEndpointsExamples.Error404Example))]
                [SwaggerResponseExample(400, typeof(ConversationalSearchEndpointsExamples.Error400Example))]
                [SwaggerResponseExample(500, typeof(ConversationalSearchEndpointsExamples.Error500Example))]
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

                    var startConversation = new StartConversation(tenant.ChatModel, tenant.AmountOfSearchReferences, (Language)request.Language);
                    var conversationId = await conversationService.StartConversationAsync(startConversation);

                    var holdConversation = new HoldConversation(conversationId.Value, tenantId, request.Prompt, request.Context, (Language)request.Language);
                    var response = await conversationService.ConverseAsync(holdConversation);

                    return MapToApiResponse(response);
                })
            .WithName("StartConversation")
            .WithDescription("Used for starting a conversation.")
            .Produces<ConversationRequest>()
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
            .Produces<ProblemDetails>((int)HttpStatusCode.InternalServerError)
            ;


        innerGroup.MapPost("/conversation/{conversationId}",
                [SwaggerRequestExample(typeof(ConversationRequest), typeof(ConversationalSearchEndpointsExamples.SuccessExample))]
                [SwaggerResponseExample(404, typeof(ConversationalSearchEndpointsExamples.Error404Example))]
                [SwaggerResponseExample(400, typeof(ConversationalSearchEndpointsExamples.Error400Example))]
                [SwaggerResponseExample(500, typeof(ConversationalSearchEndpointsExamples.Error500Example))]
                async ([FromRoute] Guid conversationId,
                    HttpContext httpContext,
                    [FromServices] IConversationService conversationService,
                    [FromBody] ConversationRequest request
                ) =>
                {
                    var tenantId = httpContext.GetTenantHeader();

                    var holdConversation = new HoldConversation(conversationId, tenantId, request.Prompt, request.Context, (Language)request.Language);
                    var response = await conversationService.ConverseAsync(holdConversation);

                    return MapToApiResponse(response);
                })
            .WithName("ContinueConversation")
            .WithDescription("Used for continuing a conversation after getting a conversationId.")
            .Produces<ConversationRequest>()
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
            .Produces<ProblemDetails>((int)HttpStatusCode.InternalServerError)
            ;
        return outerGroup;
    }

    private static ConversationReferencedResponse MapToApiResponse(ConversationReferencedResult response)
    {
        var conversationReferencedResponse = new ConversationReferencedResponse(
            new ConversationResponse(response.Result.ConversationId, response.Result.Answer, (LanguageDto)response.Result.Language),
            response.References.Select(reference => new ConversationReferenceResponse(reference.Index, reference.Url, (ConversationReferenceTypeDto)reference.Type)).ToList()
        );
        return conversationReferencedResponse;
    }

}