namespace Dan.Common.Helpers.Util;

/// <summary>
/// Logger extension
/// </summary>
public static class LoggerExtension
{
    /// <summary>
    /// Create a timer that writes the run time of the operation to the log 
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="metricName">Name of metric used in logging.</param>
    /// <returns>A Stopwatch Log that writes to the log when it is disposed</returns>
    public static StopwatchLog Timer(this ILogger logger, string metricName)
    {
        return new StopwatchLog(logger, metricName);
    }
}

/// <summary>
/// Single instance log container
/// </summary>
public class StopwatchLog : IDisposable
{
    /// <summary>
    /// The amount of milliseconds elapsed since start
    /// </summary>
    public long ElapsedMilliseconds => Stopwatch.ElapsedMilliseconds;

    private ILogger Logger { get; }

    private Stopwatch Stopwatch { get; }

    private string Logtext { get; }

    /// <summary>
    /// Create a new stopwatch log
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="text">Text to write to log. Replaces {0} with runtime until dispose.</param>
    public StopwatchLog(ILogger logger, string text)
    {
        Logger = logger;
        Stopwatch = Stopwatch.StartNew();
        Logtext = text;
    }

    /// <summary>
    /// Dispose of the Log
    /// </summary>
    public void Dispose()
    {
        Stopwatch.Stop();
        var elapsedMilliseconds = Stopwatch.ElapsedMilliseconds;
        Logger?.LogInformation("[NadobeTimer] {logtext} elapsedMs={elapsedMs}", Logtext, elapsedMilliseconds);
    }
}