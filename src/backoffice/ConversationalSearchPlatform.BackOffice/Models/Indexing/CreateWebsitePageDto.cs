using System.ComponentModel.DataAnnotations;
using ConversationalSearchPlatform.BackOffice.Models.Conversations;
using ConversationalSearchPlatform.BackOffice.Validation;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;

namespace ConversationalSearchPlatform.BackOffice.Models.Indexing;

public record CreateWebsitePageDto
{
    [Required]
    public string Name { get; set; } = default!;

    [RequiredIf("SiteMapFile", null, ErrorMessage = "Url or Sitemap file is required")]
    [UrlValidation]
    public string? Url { get; set; } = default!;

    public bool? SiteMapAvailable { get; set; }

    public bool UseUrlSiteMap { get; set; }

    [RequiredIf("Url", null, ErrorMessage = "Url or Sitemap file is required")]
    public IBrowserFile? SiteMapFile { get; set; }

    [Required]
    public ConversationReferenceTypeDto ReferenceType { get; set; }

    [Required]
    public LanguageDto Language { get; set; }
}