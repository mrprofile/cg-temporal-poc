using ConsoleExecutor.Activities;
using ConsoleExecutor.Common.Models;
using Microsoft.Extensions.Logging;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace ConsoleExecutor.Workflows;

/// <summary>
/// Workflow for executing a console application
/// </summary>
[Workflow]
public class ConsoleExecutionWorkflow
{
    // State
    private ConsoleAppParameters _parameters = null!;
    private ExecutionResult? _result;
    private string _status = "Pending";
    private int _attempts = 0;
    private Exception? _lastError;
    private bool _cancelRequested = false;

    /// <summary>
    /// Query to get the current execution status
    /// </summary>
    [WorkflowQuery]
    public string GetStatus() => _status;

    /// <summary>
    /// Query to get execution result (if available)
    /// </summary>
    [WorkflowQuery]
    public ExecutionResult? GetResult() => _result;

    /// <summary>
    /// Query to get the number of execution attempts
    /// </summary>
    [WorkflowQuery]
    public int GetAttempts() => _attempts;

    /// <summary>
    /// Signal to cancel an in-progress execution
    /// </summary>
    [WorkflowSignal]
    public async Task Cancel()
    {
        Workflow.Logger.LogInformation("Received cancellation signal");
        _cancelRequested = true;
        _status = "Cancelling";
        await Task.CompletedTask;
    }

    /// <summary>
    /// Main workflow method to execute a console application
    /// </summary>
    /// <param name="parameters">Parameters for the console application execution</param>
    /// <returns>The execution result</returns>
    [WorkflowRun]
    public async Task<ExecutionResult> RunAsync(ConsoleAppParameters parameters)
    {
        _parameters = parameters;

        Workflow.Logger.LogInformation(
            "Starting console execution workflow for {ExecutablePath} with arguments: {Arguments}",
            parameters.ExecutablePath,
            parameters.Arguments);

        try
        {
            // Check for cancellation before starting
            if (_cancelRequested)
            {
                _status = "Cancelled";
                throw new ApplicationFailureException("Execution cancelled before starting", nonRetryable: true);
            }

            _status = "Running";

            // Execute the console application and get the result
            _result = await Workflow.ExecuteActivityAsync(
                () => ConsoleExecutionActivities.ExecuteConsoleApplicationAsync(parameters),
                new()
                {
                    StartToCloseTimeout = TimeSpan.FromSeconds(parameters.TimeoutSeconds + 30), // Add buffer to the timeout
                    RetryPolicy = new()
                    {
                        MaximumAttempts = 3,
                        InitialInterval = TimeSpan.FromSeconds(5),
                        MaximumInterval = TimeSpan.FromMinutes(1),
                        BackoffCoefficient = 2,
                        NonRetryableErrorTypes = new[]
                        {
                            "System.InvalidOperationException",
                            "System.IO.FileNotFoundException"
                        }
                    }, 
                    CancellationType = ActivityCancellationType.TryCancel
                });

            // Check for cancellation after activity completion
            if (_cancelRequested)
            {
                _status = "Cancelled";
                throw new ApplicationFailureException("Execution cancelled after completion", nonRetryable: true);
            }

            _status = _result.IsSuccess ? "Completed" : "Failed";
            Workflow.Logger.LogInformation(
                "Console application execution completed with exit code {ExitCode}",
                _result.ExitCode);

            return _result;
        }
        catch (Exception ex) when (_cancelRequested)
        {
            _status = "Cancelled";
            Workflow.Logger.LogInformation("Console application execution was cancelled");
            throw new ApplicationFailureException("Execution cancelled by user", ex, nonRetryable: true);
        }
        catch (Exception ex)
        {
            _status = "Error";
            _lastError = ex;
            Workflow.Logger.LogError(ex, "Console application execution failed");
            throw;
        }
    }
}