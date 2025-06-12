using ConsoleExecutor.Common.Models;
using ConsoleExecutor.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Configure logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger<Program>();

// Create a Temporal client
logger.LogInformation("Connecting to Temporal Server...");
var targetHost = configuration["Temporal:TargetHost"] ?? "localhost:7233";
var @namespace = configuration["Temporal:Namespace"] ?? "default";

var client = await TemporalClient.ConnectAsync(
    new TemporalClientConnectOptions(targetHost)
    {
        Namespace = @namespace,
        LoggerFactory = loggerFactory
    });

// Get the task queue from config
var taskQueue = configuration["Temporal:TaskQueue"] ?? Constants.TaskQueue;

while (true)
{
    Console.Clear();
    Console.WriteLine("Console Application Executor");
    Console.WriteLine("===========================");
    Console.WriteLine();
    Console.WriteLine("1. Execute a Console Application");
    Console.WriteLine("2. Check Workflow Status");
    Console.WriteLine("3. Exit");
    Console.WriteLine();
    Console.Write("Select an option: ");
    
    var option = Console.ReadLine();
    
    switch (option)
    {
        case "1":
            await StartConsoleExecutionWorkflowAsync(client, taskQueue, logger);
            break;
        case "2":
            await CheckWorkflowStatusAsync(client, logger);
            break;
        case "3":
            logger.LogInformation("Exiting...");
            return;
        default:
            logger.LogWarning("Invalid option");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            break;
    }
}

/// <summary>
/// Starts a new console execution workflow
/// </summary>
static async Task StartConsoleExecutionWorkflowAsync(ITemporalClient client, string taskQueue, ILogger logger)
{
    Console.Clear();
    Console.WriteLine("Execute a Console Application");
    Console.WriteLine("=============================");
    Console.WriteLine();
    
    // Get console application path
    Console.Write("Enter the path to the console application: ");
    var executablePath = Console.ReadLine();
    
    if (string.IsNullOrEmpty(executablePath))
    {
        logger.LogWarning("Executable path cannot be empty");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        return;
    }
    
    // Check if executable exists
    if (!File.Exists(executablePath))
    {
        logger.LogWarning("Executable does not exist: {Path}", executablePath);
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        return;
    }
    
    // Get arguments
    Console.Write("Enter arguments (optional): ");
    var arguments = Console.ReadLine() ?? string.Empty;
    
    // Get working directory
    Console.Write("Enter working directory (optional): ");
    var workingDirectory = Console.ReadLine();
    
    // Get timeout
    Console.Write("Enter timeout in seconds (default 300): ");
    var timeoutInput = Console.ReadLine();
    var timeoutSeconds = 300;
    if (!string.IsNullOrEmpty(timeoutInput) && int.TryParse(timeoutInput, out var parsedTimeout))
    {
        timeoutSeconds = parsedTimeout;
    }
    
    // Create parameters
    var parameters = new ConsoleAppParameters
    {
        ExecutablePath = executablePath,
        Arguments = arguments,
        WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory,
        TimeoutSeconds = timeoutSeconds,
        CaptureOutput = true,
        CaptureError = true
    };
    
    // Generate a workflow ID
    var workflowId = $"console-execution-{Guid.NewGuid()}";
    
    // Start the workflow
    logger.LogInformation("Starting workflow with ID: {WorkflowId}", workflowId);
    
    try
    {
        var handle = await client.StartWorkflowAsync(
            (ConsoleExecutionWorkflow wf) => wf.RunAsync(parameters),
            new WorkflowOptions(workflowId, taskQueue)
            {
                // Set workflow timeout to the application timeout plus buffer
                ExecutionTimeout = TimeSpan.FromSeconds(timeoutSeconds + 60)
            });
        
        logger.LogInformation("Workflow started with ID: {WorkflowId}", workflowId);
        Console.WriteLine();
        Console.WriteLine($"Workflow started with ID: {workflowId}");
        Console.WriteLine("You can check the status using option 2 from the main menu.");
        
        // Ask if user wants to wait for completion
        Console.WriteLine();
        Console.Write("Wait for completion? (y/n): ");
        var waitForCompletion = Console.ReadLine()?.ToLower() == "y";
        
        if (waitForCompletion)
        {
            Console.WriteLine("Waiting for workflow to complete...");
            
            try
            {
                var result = await handle.GetResultAsync();
                
                Console.WriteLine();
                Console.WriteLine($"Workflow completed with exit code: {result.ExitCode}");
                Console.WriteLine($"Execution time: {result.ExecutionTimeMs}ms");
                
                Console.WriteLine();
                Console.WriteLine("Standard Output:");
                Console.WriteLine("---------------");
                Console.WriteLine(result.StandardOutput);
                
                if (!string.IsNullOrEmpty(result.StandardError))
                {
                    Console.WriteLine();
                    Console.WriteLine("Standard Error:");
                    Console.WriteLine("--------------");
                    Console.WriteLine(result.StandardError);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error waiting for workflow result");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error starting workflow");
        Console.WriteLine($"Error: {ex.Message}");
    }
    
    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}

/// <summary>
/// Checks the status of an existing workflow
/// </summary>
static async Task CheckWorkflowStatusAsync(ITemporalClient client, ILogger logger)
{
    Console.Clear();
    Console.WriteLine("Check Workflow Status");
    Console.WriteLine("====================");
    Console.WriteLine();
    
    // Get workflow ID
    Console.Write("Enter workflow ID: ");
    var workflowId = Console.ReadLine();
    
    if (string.IsNullOrEmpty(workflowId))
    {
        logger.LogWarning("Workflow ID cannot be empty");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
        return;
    }
    
    try
    {
        var handle = client.GetWorkflowHandle<ConsoleExecutionWorkflow>(workflowId);
        
        // Query workflow status
        var status = await handle.QueryAsync(wf => wf.GetStatus());
        var attempts = await handle.QueryAsync(wf => wf.GetAttempts());
        
        Console.WriteLine();
        Console.WriteLine($"Status: {status}");
        Console.WriteLine($"Attempts: {attempts}");
        
        // Get result if workflow is complete
        if (status is "Completed" or "Failed")
        {
            try
            {
                var result = await handle.QueryAsync(wf => wf.GetResult());
                
                if (result != null)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Exit code: {result.ExitCode}");
                    Console.WriteLine($"Execution time: {result.ExecutionTimeMs}ms");
                    
                    Console.WriteLine();
                    Console.WriteLine("Standard Output:");
                    Console.WriteLine("---------------");
                    Console.WriteLine(result.StandardOutput);
                    
                    if (!string.IsNullOrEmpty(result.StandardError))
                    {
                        Console.WriteLine();
                        Console.WriteLine("Standard Error:");
                        Console.WriteLine("--------------");
                        Console.WriteLine(result.StandardError);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving workflow result");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        else if (status == "Running")
        {
            // Option to cancel
            Console.WriteLine();
            Console.Write("Do you want to cancel this workflow? (y/n): ");
            var cancelWorkflow = Console.ReadLine()?.ToLower() == "y";
            
            if (cancelWorkflow)
            {
                try
                {
                    await handle.CancelAsync();
                    Console.WriteLine("Cancellation signal sent");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error cancelling workflow");
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error checking workflow status");
        Console.WriteLine($"Error: {ex.Message}");
    }
    
    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
}