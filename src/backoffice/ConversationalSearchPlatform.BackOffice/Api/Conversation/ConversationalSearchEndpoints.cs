using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ConversationalSearchPlatform.BackOffice.Api.Conversation.Examples;
using ConversationalSearchPlatform.BackOffice.Api.Extensions;
using ConversationalSearchPlatform.BackOffice.Api.WebSockets;
using ConversationalSearchPlatform.BackOffice.Exceptions;
using ConversationalSearchPlatform.BackOffice.Models.Conversations;
using ConversationalSearchPlatform.BackOffice.Services;
using ConversationalSearchPlatform.BackOffice.Services.Models;
using ConversationalSearchPlatform.BackOffice.Services.Models.ConversationDebug;
using ConversationalSearchPlatform.BackOffice.Tenants;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using Swashbuckle.AspNetCore.Filters;

namespace ConversationalSearchPlatform.BackOffice.Api.Conversation;

public static class ConversationalSearchEndpoints
{
    private const string ConversationalSearchTag = "Conversational Search";

    public static IEndpointRouteBuilder MapConversationalSearchWebSocket(this IEndpointRouteBuilder outerGroup)
    {
        outerGroup.Map("ws",
            async (
                HttpContext httpContext,
                [FromServices] IConversationService conversationService,
                [FromServices] IMultiTenantStore<ApplicationTenantInfo> tenantStore,
                [FromServices] ILoggerFactory loggerFactory
            ) =>
            {
                using var ws = await httpContext.WebSockets.AcceptWebSocketAsync();
                //creating logger manually because this is a static class
                var logger = loggerFactory.CreateLogger("ConversationalSearchEndpoints");

                if (!httpContext.WebSockets.IsWebSocketRequest)
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var tenantId = httpContext.GetTenantHeader();
                var tenant = await tenantStore.TryGetAsync(tenantId);

                if (tenant == null)
                {
                    ThrowHelper.ThrowTenantNotFoundException(tenantId);
                }

                await WebSocketUtils.ReceiveMessage(ws,
                    async (buffer, count) =>
                        await HandleWebSocketMessage(buffer, count, logger, tenant, conversationService, ws));
            });

        return outerGroup;
    }


    public static IEndpointRouteBuilder MapConversationalSearchGroup(this IEndpointRouteBuilder outerGroup)
    {
        var innerGroup = outerGroup.MapGroup(string.Empty)
            .RequireAuthorization(nameof(TenantApiKeyHeaderRequirement))
            .WithTags(ConversationalSearchTag);

        innerGroup.MapPost($"/conversation",
                [SwaggerRequestExample(typeof(StartConversationRequest), typeof(ConversationalSearchEndpointsExamples.StartConversationRequestSuccess))]
                [SwaggerResponseExample(200, typeof(ConversationalSearchEndpointsExamples.StartConversationResponseSuccess))]
                [SwaggerResponseExample(404, typeof(ConversationalSearchEndpointsExamples.Error404Example))]
                [SwaggerResponseExample(400, typeof(ConversationalSearchEndpointsExamples.Error400Example))]
                [SwaggerResponseExample(500, typeof(ConversationalSearchEndpointsExamples.Error500Example))]
                async (
                    HttpContext httpContext,
                    [FromServices] IConversationService conversationService,
                    [FromServices] IMultiTenantStore<ApplicationTenantInfo> tenantStore,
                    [FromBody] StartConversationRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var tenantId = httpContext.GetTenantHeader();
                    var tenant = await tenantStore.TryGetAsync(tenantId);

                    if (tenant == null)
                    {
                        ThrowHelper.ThrowTenantNotFoundException(tenantId);
                    }

                    return await HandleStartConversationAsync(tenant, request, conversationService, cancellationToken);
                })
            .WithName("StartConversation")
            .WithDescription("Used for starting a conversation.")
            .Produces<ConversationReferencedResult>()
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
            .Produces<ProblemDetails>((int)HttpStatusCode.InternalServerError)
            ;


        innerGroup.MapPost("/conversation/{conversationId}",
                [SwaggerRequestExample(typeof(ConversationRequest), typeof(ConversationalSearchEndpointsExamples.SuccessExample))]
                [SwaggerResponseExample(200, typeof(ConversationalSearchEndpointsExamples.ConversationReferencedResponseExample))]
                [SwaggerResponseExample(404, typeof(ConversationalSearchEndpointsExamples.Error404Example))]
                [SwaggerResponseExample(400, typeof(ConversationalSearchEndpointsExamples.Error400Example))]
                [SwaggerResponseExample(500, typeof(ConversationalSearchEndpointsExamples.Error500Example))]
                async ([FromRoute] Guid conversationId,
                    HttpContext httpContext,
                    [FromServices] IConversationService conversationService,
                    [FromBody] ConversationRequest request,
                    CancellationToken cancellationToken
                ) =>
                {
                    var tenantId = httpContext.GetTenantHeader();

                    return await HandleHoldConversationAsync(conversationId, tenantId, request, conversationService, cancellationToken);
                })
            .WithName("ContinueConversation")
            .WithDescription("Used for continuing a conversation after getting a conversationId.")
            .Produces<ConversationReferencedResult>()
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
            .Produces<ProblemDetails>((int)HttpStatusCode.InternalServerError)
            ;

        innerGroup.MapPost("/conversation/{conversationId}/streaming",
                [SwaggerRequestExample(typeof(ConversationRequest), typeof(ConversationalSearchEndpointsExamples.SuccessExample))]
                [SwaggerResponseExample(200, typeof(ConversationalSearchEndpointsExamples.ConversationReferencedResponseExample))]
                [SwaggerResponseExample(404, typeof(ConversationalSearchEndpointsExamples.Error404Example))]
                [SwaggerResponseExample(400, typeof(ConversationalSearchEndpointsExamples.Error400Example))]
                [SwaggerResponseExample(500, typeof(ConversationalSearchEndpointsExamples.Error500Example))]
                async ([FromRoute] Guid conversationId,
                    [FromServices] IConversationService conversationService,
                    [FromBody] ConversationRequest request,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
                    var httpContextResponse = httpContext.Response;
                    httpContextResponse.ContentType = "text; charset=utf-8";

                    var tenantId = httpContext.GetTenantHeader();

                    var holdConversation = new HoldConversation(conversationId, tenantId, new UserChatMessage(request.Prompt), request.Context, request.Debug, (Language)request.Language);

                    await foreach (var crr in conversationService
                                       .ConverseStreamingAsync(
                                           holdConversation,
                                           cancellationToken))
                    {
                        var mapped = MapToApiResponse(crr);
                        var bytes = JsonSerializer.SerializeToUtf8Bytes(mapped);
                        await httpContextResponse.BodyWriter.WriteAsync(new ReadOnlyMemory<byte>(bytes), cancellationToken);
                        await httpContextResponse.BodyWriter.FlushAsync(cancellationToken);
                    }
                }).WithName("ContinueConversationStreaming")
            .WithDescription("Used for continuing a conversation after getting a conversationId in a streaming manner.")
            .Produces<ConversationReferencedResult>()
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
            .Produces<ProblemDetails>((int)HttpStatusCode.InternalServerError);

        innerGroup.MapGet("/conversation/context",
                [SwaggerResponseExample(200, typeof(ConversationalSearchEndpointsExamples.ConversationReferencedResponseExample))]
                [SwaggerResponseExample(404, typeof(ConversationalSearchEndpointsExamples.Error404Example))]
                [SwaggerResponseExample(400, typeof(ConversationalSearchEndpointsExamples.Error400Example))]
                [SwaggerResponseExample(500, typeof(ConversationalSearchEndpointsExamples.Error500Example))]
                async ([FromServices] IConversationService conversationService, HttpContext httpContext) =>
                {
                    var tenantId = httpContext.GetTenantHeader();

                    var conversationContext = await conversationService.GetConversationContext(new GetConversationContext(tenantId));
                    return new ConversationContextResponse(conversationContext.Variables);
                }).WithName("GetConversationContext")
            .WithDescription("Used to get context variables which can be used to replace parts of the prompt")
            .Produces<ConversationContextResponse>()
            .Produces<ProblemDetails>((int)HttpStatusCode.NotFound)
            .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
            .Produces<ProblemDetails>((int)HttpStatusCode.InternalServerError);

        return outerGroup;
    }

    private static async Task<ConversationReferencedResponse> HandleHoldConversationAsync(Guid conversationId, string tenantId, ConversationRequest request,
        IConversationService conversationService,
        CancellationToken cancellationToken)
    {
        var holdConversation = new HoldConversation(conversationId, tenantId, new UserChatMessage(request.Prompt), request.Context, request.Debug, (Language)request.Language);
        var response = await conversationService.ConverseAsync(holdConversation, cancellationToken);

        return MapToApiResponse(response);
    }

    private static async Task<StartConversationResponse> HandleStartConversationAsync(ApplicationTenantInfo tenant, StartConversationRequest request,
        IConversationService conversationService,
        CancellationToken cancellationToken)
    {
        var startConversation = new StartConversation(tenant.ChatModel, tenant.AmountOfSearchReferences, (Language)request.Language);
        var conversationId = await conversationService.StartConversationAsync(startConversation, cancellationToken);
        return new StartConversationResponse(conversationId.Value);
    }

    private static ConversationReferencedResponse MapToApiResponse(ConversationReferencedResult response)
    {
        var conversationReferencedResponse = new ConversationReferencedResponse(
            MapConversationToApiResponse(response),
            MapReferencesToApiResponse(response.References),
            response.EndConversation,
            MapDebugInformationToApiResponse(response.DebugInformation)
        );
        return conversationReferencedResponse;
    }

    private static ConversationResponse MapConversationToApiResponse(ConversationReferencedResult response) =>
        new(response.Result.ConversationId, response.Result.Answer, (LanguageDto)response.Result.Language);

    private static DebugInformationResponse? MapDebugInformationToApiResponse(DebugInformation? debugInformation)
    {
        if (debugInformation == null)
        {
            return null;
        }

        return new DebugInformationResponse(debugInformation.DebugRecords
            .Select(record => new DebugRecordResponse
            {
                References = new ReferencesResponse
                {
                    Text = record.References.Text.Select(info => new TextDebugInfoResponse(info.InternalId, info.UsedInAnswer, info.Source, info.Content)).ToList(),
                    Image = record.References.Image.Select(info => new ImageDebugInfoResponse(info.InternalId, info.UsedInAnswer, info.Source, info.AltDescription)).ToList()
                },
                ExecutedAt = record.ExecutedAt,
                ReplacedContextVariables = record.ReplacedContextVariables,
                FullPrompt = record.FullPrompt
            })
            .ToList(), debugInformation.UsedInputTokens, debugInformation.UsedOutputTokens);
    }

    private static List<ConversationReferenceResponse> MapReferencesToApiResponse(IEnumerable<ConversationReference> references) =>
        references.Select(reference => new ConversationReferenceResponse(reference.Index, reference.Url, reference.Type, reference.Title)).ToList();

    private static async Task HandleWebSocketMessage(byte[] buffer, int count, ILogger logger, ApplicationTenantInfo tenant, IConversationService conversationService, WebSocket ws)
    {
        var message = Encoding.UTF8.GetString(buffer, 0, count);

        try
        {
            var requestType = JsonNode.Parse(message)?.AsObject()["Type"]?.ToString();

            if (requestType == null)
            {
                logger.LogError("Cannot determine request type");
                return;
            }

            var parsed = Enum.TryParse<ConversationEndpointType>(requestType, out var parsedType);

            if (!parsed)
            {
                logger.LogError("Cannot determine request type based on {Type}", requestType);
                return;
            }

            switch (parsedType)
            {
                case ConversationEndpointType.StartConversation:
                    await HandleStartConversationWebSocketMessage(message, tenant, conversationService, ws);
                    break;
                case ConversationEndpointType.HoldConversation:
                    await HandleHoldConversationWebSocketMessage(message, tenant, conversationService, ws);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Unable to process request");
        }
    }

    private static async Task HandleStartConversationWebSocketMessage(
        string jsonDocument,
        ApplicationTenantInfo tenant,
        IConversationService conversationService,
        WebSocket ws)
    {
        var request = JsonSerializer.Deserialize<StartConversationRequest>(jsonDocument)!;
        var conversationId = await HandleStartConversationAsync(tenant, request, conversationService, new CancellationToken());
        var responseMessage = JsonSerializer.Serialize(conversationId);
        await WebSocketUtils.SendResponse(ws, responseMessage);
    }

    private static async Task HandleHoldConversationWebSocketMessage(
        string jsonDocument,
        ApplicationTenantInfo tenant,
        IConversationService conversationService,
        WebSocket ws)
    {
        var request = JsonSerializer.Deserialize<ConversationRequest>(jsonDocument)!;

        if (request.ConversationId == null)
        {
            return;
        }

        var tenantId = tenant.Id!;
        var holdConversation = new HoldConversation(request.ConversationId.Value, tenantId, new UserChatMessage(request.Prompt), request.Context, request.Debug, (Language)request.Language);

        await foreach (var crr in conversationService
                           .ConverseStreamingAsync(
                               holdConversation,
                               new CancellationToken()))
        {
            var responseMessage = JsonSerializer.Serialize(crr);
            await WebSocketUtils.SendResponse(ws, responseMessage);
        }
    }

}