using System.Text.Json.Serialization;

namespace Bacon.Scheduler.Samples.Models;

public class CleanUpLog
{
    /// <summary>
    /// Defines the unique identifier of the data source job log
    /// </summary>
    [JsonPropertyName("clean_up_log_id")]
    public long CleanUpLogId { get; set; }

    /// <summary>
    /// Defines the unique identifier of the data source
    /// </summary>
    [JsonPropertyName("toto_id")]
    public Guid TotoId { get; set; }
}