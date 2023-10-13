using System.ComponentModel.DataAnnotations;
using ConversationalSearchPlatform.BackOffice.Validation;

namespace ConversationalSearchPlatform.BackOffice.Models.Indexing;

public record UpdateWebsitePageDto
{
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    [UrlValidation]
    public string Url { get; set; }
}