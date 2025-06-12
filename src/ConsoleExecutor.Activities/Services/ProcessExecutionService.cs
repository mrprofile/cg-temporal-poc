using System.Diagnostics;
using ConsoleExecutor.Common.Models;

namespace ConsoleExecutor.Activities.Services;

/// <summary>
/// Service for executing console applications as processes
/// </summary>
public class ProcessExecutionService
{
    /// <summary>
    /// Executes a console application with the specified parameters
    /// </summary>
    /// <param name="parameters">Parameters for the console application execution</param>
    /// <param name="cancellationToken">Cancellation token to cancel the execution</param>
    /// <returns>The execution result</returns>
    public async Task<ExecutionResult> ExecuteProcessAsync(
        ConsoleAppParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Validate that the executable exists
        if (!File.Exists(parameters.ExecutablePath))
        {
            throw new FileNotFoundException(
                $"Executable not found: {parameters.ExecutablePath}",
                parameters.ExecutablePath);
        }

        // Set up process start info
        var startInfo = new ProcessStartInfo
        {
            FileName = parameters.ExecutablePath,
            Arguments = parameters.Arguments,
            UseShellExecute = false,
            RedirectStandardOutput = parameters.CaptureOutput,
            RedirectStandardError = parameters.CaptureError,
            CreateNoWindow = true
        };

        // Set working directory if specified
        if (!string.IsNullOrEmpty(parameters.WorkingDirectory))
        {
            if (!Directory.Exists(parameters.WorkingDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Working directory not found: {parameters.WorkingDirectory}");
            }

            startInfo.WorkingDirectory = parameters.WorkingDirectory;
        }

        // Add environment variables
        foreach (var variable in parameters.EnvironmentVariables)
        {
            startInfo.EnvironmentVariables[variable.Key] = variable.Value;
        }

        // Start the process
        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var standardOutput = new List<string>();
        var standardError = new List<string>();

        // Capture output if requested
        if (parameters.CaptureOutput)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    standardOutput.Add(e.Data);
                }
            };
        }

        // Capture error if requested
        if (parameters.CaptureError)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    standardError.Add(e.Data);
                }
            };
        }

        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        process.Start();

        // Begin output/error redirection
        if (parameters.CaptureOutput)
        {
            process.BeginOutputReadLine();
        }

        if (parameters.CaptureError)
        {
            process.BeginErrorReadLine();
        }

        // Create a task that completes when the process exits
        var processCompletion = process.WaitForExitAsync(cancellationToken);

        // Create a task that completes after the timeout
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(parameters.TimeoutSeconds), cancellationToken);

        // Wait for either the process to complete or the timeout to occur
        var completedTask = await Task.WhenAny(processCompletion, timeoutTask);

        // If the timeout task completed first, kill the process
        if (completedTask == timeoutTask)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                // Ignore exceptions when trying to kill the process
                Console.Error.WriteLine($"Error killing process: {ex.Message}");
            }

            throw new TimeoutException(
                $"The process did not complete within the specified timeout of {parameters.TimeoutSeconds} seconds.");
        }

        // Get the results
        stopwatch.Stop();
        var endTime = DateTime.UtcNow;
        var exitCode = process.ExitCode;

        return new ExecutionResult
        {
            ExitCode = exitCode,
            StandardOutput = string.Join(Environment.NewLine, standardOutput),
            StandardError = string.Join(Environment.NewLine, standardError),
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            StartTime = startTime,
            EndTime = endTime
        };
    }
}