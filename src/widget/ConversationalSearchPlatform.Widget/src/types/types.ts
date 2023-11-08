export const ROLES = ["user", "assistant"] as const;

export type Context = Record<string, string>

export type Language = "English" | "Swedish" | "Dutch"

export type Message = {
    role: (typeof ROLES)[number];
    content: string;
};

export interface StartConversationRequest {
    language: Language
    type: "StartConversation"
}

export interface StartConversationResponse {
    conversationId: string
}

export interface HoldConversationRequest {
    conversationId: string
    prompt: string
    context: Context
    debug: boolean
    language: Language
    type: "HoldConversation"
}

export interface HoldConversationResponse {
    response: InnerHoldConversationResponse
    references: HoldConversationReference[]
}

export interface InnerHoldConversationResponse {
    conversationId: string
    answer: string
    language: string
}

export interface HoldConversationReference {
    index: number
    url: string
    type: string
    title: string
}

export interface ConversationContextResponse {
    variables: string[]
}

export interface Configuration {
    apiKey: string
    apiUrl: string
}