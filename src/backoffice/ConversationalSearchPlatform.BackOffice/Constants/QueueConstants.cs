namespace ConversationalSearchPlatform.BackOffice.Constants;

public static class QueueConstants
{
    public const string TelemetryQueue = "telemetry_queue";
    public const string IndexingQueue = "indexing_queue";
    public const string DailyPricingQueue = "daily_pricing_queue";

    public readonly static string[] Queues = new []
    {
        TelemetryQueue,
        IndexingQueue,
        DailyPricingQueue
    };

}