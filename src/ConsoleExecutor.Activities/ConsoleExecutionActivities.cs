using ConsoleExecutor.Activities.Services;
using ConsoleExecutor.Common.Models;
using Microsoft.Extensions.Logging;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace ConsoleExecutor.Activities;

/// <summary>
/// Activities for executing console applications
/// </summary>
public static class ConsoleExecutionActivities
{
    private static readonly ProcessExecutionService _processService = new();

    /// <summary>
    /// Activity to execute a console application
    /// </summary>
    /// <param name="parameters">Parameters for the console application execution</param>
    /// <returns>The execution result</returns>
    [Activity]
    public static async Task<ExecutionResult> ExecuteConsoleApplicationAsync(ConsoleAppParameters parameters)
    {
        var context = ActivityExecutionContext.Current;
        context.Logger.LogInformation(
            "Executing console application: {ExecutablePath} {Arguments}",
            parameters.ExecutablePath,
            parameters.Arguments);

        try
        {
            // Execute the process with heartbeating for long-running executions
            var result = await _processService.ExecuteProcessAsync(
                parameters,
                context.CancellationToken);

            // Log results
            context.Logger.LogInformation(
                "Console application completed with exit code {ExitCode}",
                result.ExitCode);

            if (!result.IsSuccess)
            {
                context.Logger.LogWarning(
                    "Console application failed with exit code {ExitCode}. Error output: {Error}",
                    result.ExitCode,
                    result.StandardError);
            }

            return result;
        }
        catch (Exception ex)
        {
            context.Logger.LogError(
                ex,
                "Failed to execute console application: {ExecutablePath}",
                parameters.ExecutablePath);

            throw new ApplicationFailureException(
                $"Console application execution failed: {ex.Message}",
                ex,
                nonRetryable: ex is FileNotFoundException || ex is DirectoryNotFoundException);
        }
    }
}