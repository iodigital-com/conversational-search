import {
    Configuration,
    Context, ConversationContextResponse,
    HoldConversationRequest,
    StartConversationRequest,
    StartConversationResponse
} from "@/types/types.ts";
import {http, httpStreaming} from "@/services/ApiHelper.ts";


export const startConversation = async (configuration: Configuration) => {
    const requestBody: StartConversationRequest = {
        language: "English",
        type: "StartConversation"
    };

    const response = await http<StartConversationResponse>(
        new Request(`${configuration.apiUrl}/conversation`,
            {
                body: JSON.stringify(requestBody),
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-API-Key": configuration.apiKey
                }
            })
    );

    return response.parsedBody;
};


export const holdConversation = async (
    conversationId: string,
    prompt: string,
    context: Context,
    callback: (callbackValue: string) => Promise<void>,
    abortSignal: AbortSignal,
    configuration: Configuration) => {
    const requestBody: HoldConversationRequest = {
        language: "English",
        debug: false,
        conversationId: conversationId,
        prompt: prompt,
        context: context,
        type: "HoldConversation"
    };

    return await httpStreaming<HoldConversationRequest>(
        new Request(`${configuration.apiUrl}/conversation/${conversationId}/streaming`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "X-API-Key": configuration.apiKey
                },
                body: JSON.stringify(requestBody),
                signal: abortSignal
            }),
        callback
    );
};

export const getConversationContext = async (configuration: Configuration) => {
    const response = await http<ConversationContextResponse>(
        new Request(`${configuration.apiUrl}/conversation/context`,
            {
                method: "GET",
                headers: {
                    "Content-Type": "application/json",
                    "X-API-Key": configuration.apiKey
                }
            })
    );
    return response.parsedBody;
};