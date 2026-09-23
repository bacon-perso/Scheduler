using System.Text.Json.Serialization;

namespace Bacon.Scheduler.Samples.Models;

public class JobLog
{
    /// <summary>
    /// Defines the unique identifier of the data source job log
    /// </summary>
    [JsonPropertyName("source_job_log_id")]
    public long DataSourceJobLogId { get; set; }

    /// <summary>
    /// Defines the unique identifier of the data source
    /// </summary>
    [JsonPropertyName("source_id")]
    public Guid DataSourceId { get; set; }

    /// <summary>
    /// Indicates the number of Rows that was Inserted during the Data Source Execution
    /// </summary>
    [JsonPropertyName("rows_inserted")]
    public int RowsInserted { get; set; }

    /// <summary>
    /// Indicates the number of Rows that was Updated during the Data Source Execution
    /// </summary>
    [JsonPropertyName("rows_updated")]
    public int RowsUpdated { get; set; }

    /// <summary>
    /// Indicates the number of Rows that was Deleted during the Data Source Execution
    /// </summary>
    [JsonPropertyName("rows_deleted")]
    public int RowsDeleted { get; set; }
}