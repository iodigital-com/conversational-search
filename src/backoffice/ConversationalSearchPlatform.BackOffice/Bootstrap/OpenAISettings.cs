namespace ConversationalSearchPlatform.BackOffice.Bootstrap;

public record OpenAISettings(bool UseAzure, string ApiKey, string ResourceName, string VersionForChat);