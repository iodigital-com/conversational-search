namespace ConversationalSearchPlatform.BackOffice.Models.Conversations;

/// <summary>
/// Contains a reference.
/// </summary>
/// <param name="Index">Index used in the answer for this reference</param>
/// <param name="Url">The source of the reference</param>
/// <param name="Type">The type of the reference</param>
/// <param name="Title">Title of the page</param>
public record ConversationReferenceResponse(int Index, string Url, ConversationReferenceTypeDto Type, string Title);