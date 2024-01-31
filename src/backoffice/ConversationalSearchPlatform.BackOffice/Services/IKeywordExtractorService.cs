namespace ConversationalSearchPlatform.BackOffice.Services;

public interface IKeywordExtractorService
{
	public Task<List<string>> ExtractKeywordAsync(string text);
}
