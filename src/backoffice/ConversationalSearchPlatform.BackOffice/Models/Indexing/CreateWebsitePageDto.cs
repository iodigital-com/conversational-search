using System.ComponentModel.DataAnnotations;
using ConversationalSearchPlatform.BackOffice.Models.Conversations;
using ConversationalSearchPlatform.BackOffice.Validation;

namespace ConversationalSearchPlatform.BackOffice.Models.Indexing;

public record CreateWebsitePageDto
{
    [Required]
    public string Name { get; set; } = default!;

    [Required]
    [UrlValidation]
    public string Url { get; set; } = default!;

    [Required]
    public ConversationReferenceTypeDto ReferenceType { get; set; }

    [Required]
    public LanguageDto Language { get; set; }
}