namespace ConsoleExecutor.Common.Models;

/// <summary>
/// Result of a console application execution
/// </summary>
public record ExecutionResult
{
    /// <summary>
    /// Exit code returned by the process
    /// </summary>
    public int ExitCode { get; init; }
    
    /// <summary>
    /// Standard output captured from the process
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;
    
    /// <summary>
    /// Standard error captured from the process
    /// </summary>
    public string StandardError { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the process completed successfully (exit code 0)
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
    
    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; init; }
    
    /// <summary>
    /// Timestamp when the execution started
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// Timestamp when the execution completed
    /// </summary>
    public DateTime EndTime { get; init; }
}