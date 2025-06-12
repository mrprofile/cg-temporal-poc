
using ConsoleExecutor.Activities;
using ConsoleExecutor.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Worker;

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

// Set up cancellation to gracefully shutdown worker when CTRL+C is pressed
using var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    tokenSource.Cancel();
    eventArgs.Cancel = true;
};

// Create a worker
logger.LogInformation("Creating Temporal Worker...");
var taskQueue = configuration["Temporal:TaskQueue"] ?? Constants.TaskQueue;

using var worker = new TemporalWorker(
    client,
    new TemporalWorkerOptions(taskQueue)
        .AddWorkflow<ConsoleExecutionWorkflow>()
        .AddActivity(ConsoleExecutionActivities.ExecuteConsoleApplicationAsync));

// Run the worker
logger.LogInformation("Starting worker on task queue: {TaskQueue}", taskQueue);
try
{
    await worker.ExecuteAsync(tokenSource.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Worker shutdown requested");
}
catch (Exception ex)
{
    logger.LogError(ex, "Worker failed with error");
}

logger.LogInformation("Worker stopped");