namespace ConsoleExecutor.Common.Models;

/// <summary>
/// Parameters for executing a console application
/// </summary>
public record ConsoleAppParameters
{
    /// <summary>
    /// Full path to the console application executable
    /// </summary>
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the console application
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Working directory for the console application (optional)
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Maximum time in seconds to wait for the application to complete
    /// </summary>
    public int TimeoutSeconds { get; init; } = 300; // Default: 5 minutes

    /// <summary>
    /// Environment variables to set for the process
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();

    /// <summary>
    /// Whether to capture standard output from the console application
    /// </summary>
    public bool CaptureOutput { get; init; } = true;

    /// <summary>
    /// Whether to capture standard error from the console application
    /// </summary>
    public bool CaptureError { get; init; } = true;
}