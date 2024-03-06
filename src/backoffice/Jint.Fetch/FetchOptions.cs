namespace Jint.Fetch
{
    public struct FetchOptions()
    {
        public static FetchOptions Default => new FetchOptions();

        public string Method { get; set; } = "GET";
        public string Referrer { get; set; } = "jint";

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public object Body { get; set; } = "";
    }
}
