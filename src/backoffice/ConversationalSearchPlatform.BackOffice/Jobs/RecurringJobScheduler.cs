using Hangfire;

namespace ConversationalSearchPlatform.BackOffice.Jobs;

public class RecurringJobScheduler : BackgroundService
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<RecurringJobScheduler> _logger;

    public RecurringJobScheduler(IRecurringJobManager recurringJobManager, ILogger<RecurringJobScheduler> logger, IBackgroundJobClient backgroundJobClient)
    {
        _recurringJobManager = recurringJobManager;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduling jobs");

        ScheduleOpenAIPricingJob();

        _logger.LogInformation("Finished Scheduling jobs");

        return Task.CompletedTask;
    }

    private void ScheduleOpenAIPricingJob()
    {
        const string recurringJobId = $"{nameof(OpenAIPricingJob)}-recurring";
        _logger.LogInformation("Scheduling {JobName}", recurringJobId);
        
        _backgroundJobClient.Enqueue<OpenAIPricingJob>(x => x.Execute());
        _recurringJobManager.AddOrUpdate<OpenAIPricingJob>(
            recurringJobId,
            x => x.Execute(),
            Cron.Daily(2));
    }
}