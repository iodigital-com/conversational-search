
namespace Jint.Fetch
{
    public static class FetchBootstrap
    {
        public static Engine AddFetchFunctionality(this Engine engine)
        {
            return engine.SetValue("fetch", new Func<string, object, Task<FetchResult>>((uri, options) => FetchClass.Fetch(uri, FetchClass.ExpandoToOptionsObject(options))));
        }
    }
}
