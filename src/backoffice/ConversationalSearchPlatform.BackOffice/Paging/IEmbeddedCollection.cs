namespace ConversationalSearchPlatform.BackOffice.Paging;

/// <summary>
/// In your implementation, set a custom json property name for the resourceList by using the JsonPropertyName attribute (from System.Text.Json)
/// or JsonProperty attribute (from Newtonsoft.Json) depending on your serializer.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IEmbeddedCollection<T>
    where T : class
{
    /// <summary>
    /// Requested data.
    /// </summary>
    IEnumerable<T> ResourceList { get; set; }
}
