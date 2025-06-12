# Temporal.io Console Application Executor

A robust .NET solution for executing console applications reliably using Temporal.io workflow orchestration. This project demonstrates how to leverage Temporal's durable execution capabilities to run external console applications with built-in retry mechanisms, timeout handling, and comprehensive monitoring.

## Features

- ✅ **Reliable Execution**: Automatic retries and fault tolerance for console application execution
- ✅ **Timeout Management**: Configurable timeouts with process termination capabilities
- ✅ **Output Capture**: Capture and store standard output and error streams
- ✅ **Environment Control**: Set working directories and environment variables
- ✅ **Real-time Monitoring**: Query workflow status and progress in real-time
- ✅ **Cancellation Support**: Cancel long-running executions via signals
- ✅ **Comprehensive Logging**: Detailed logging throughout the execution lifecycle
- ✅ **Web UI Integration**: Monitor executions through Temporal's Web UI

## Prerequisites

- .NET 7.0 or later
- Docker and Docker Compose (for running Temporal server)
- Git (for cloning repositories)

## Project Structure

```
ConsoleExecutor/
├── ConsoleExecutor.sln
├── README.md
├── .gitignore
└── src/
    ├── ConsoleExecutor.Common/
    │   ├── ConsoleExecutor.Common.csproj
    │   ├── Constants.cs
    │   └── Models/
    │       ├── ConsoleAppParameters.cs
    │       └── ExecutionResult.cs
    ├── ConsoleExecutor.Workflows/
    │   ├── ConsoleExecutor.Workflows.csproj
    │   └── ConsoleExecutionWorkflow.cs
    ├── ConsoleExecutor.Activities/
    │   ├── ConsoleExecutor.Activities.csproj
    │   ├── ConsoleExecutionActivities.cs
    │   └── Services/
    │       └── ProcessExecutionService.cs
    ├── ConsoleExecutor.Worker/
    │   ├── ConsoleExecutor.Worker.csproj
    │   ├── Program.cs
    │   └── appsettings.json
    └── ConsoleExecutor.Client/
        ├── ConsoleExecutor.Client.csproj
        ├── Program.cs
        └── appsettings.json
```

## Quick Start

### Set Up Temporal Server

First, start a local Temporal server using Docker:

```bash
# Clone the Temporal Docker Compose repository
git clone https://github.com/temporalio/docker-compose.git
cd docker-compose

# Start Temporal server with all dependencies
docker compose up
```

The Temporal Web UI will be available at: http://localhost:8080

## Usage

### Running a Console Application

1. Start the client application
2. Select option "1. Execute a Console Application"
3. Provide the following information:
   - **Executable Path**: Full path to your console application
   - **Arguments**: Command-line arguments (optional)
   - **Working Directory**: Working directory for execution (optional)
   - **Timeout**: Maximum execution time in seconds

### Monitoring Execution

You can monitor executions in several ways:

1. **Client Application**: Use option "2. Check Workflow Status" to query execution status
2. **Temporal Web UI**: Visit http://localhost:8080 to see all workflows and their execution history
3. **Logs**: Check worker logs for detailed execution information

### Example Usage Scenarios

#### Execute a PowerShell Script
```
Executable Path: C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe
Arguments: -File "C:\Scripts\MyScript.ps1" -Parameter "Value"
Working Directory: C:\Scripts
Timeout: 600
```

#### Execute a .NET Console Application
```
Executable Path: C:\MyApp\MyConsoleApp.exe
Arguments: --input "data.txt" --output "result.txt"
Working Directory: C:\MyApp
Timeout: 300
```

#### Execute a Python Script
```
Executable Path: python
Arguments: myscript.py --verbose
Working Directory: C:\PythonScripts
Timeout: 1800
```

## Configuration

### Worker Configuration (appsettings.json)

```json
{
  "Temporal": {
    "TargetHost": "localhost:7233",
    "Namespace": "default",
    "TaskQueue": "console-execution-queue"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### Client Configuration (appsettings.json)

```json
{
  "Temporal": {
    "TargetHost": "localhost:7233",
    "Namespace": "default",
    "TaskQueue": "console-execution-queue"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## Architecture

### Components

1. **ConsoleExecutor.Common**: Shared models and constants
2. **ConsoleExecutor.Workflows**: Workflow definitions for orchestrating console application execution
3. **ConsoleExecutor.Activities**: Activities that perform the actual console application execution
4. **ConsoleExecutor.Worker**: Worker process that hosts and executes workflows and activities
5. **ConsoleExecutor.Client**: Client application for starting and monitoring workflow executions

### Workflow Flow

1. **Client** starts a workflow execution with console application parameters
2. **Workflow** orchestrates the execution and maintains state
3. **Activity** executes the console application using .NET's Process class
4. **Results** are captured and returned through the workflow
5. **Monitoring** is available through queries and the Temporal Web UI

### Key Features

- **Retry Logic**: Automatic retries with exponential backoff for transient failures
- **Timeout Handling**: Process termination if execution exceeds specified timeout
- **Output Capture**: Standard output and error streams are captured and stored
- **Heartbeating**: Long-running activities send heartbeats to prevent timeouts
- **Cancellation**: Workflows can be cancelled via signals
- **Error Handling**: Comprehensive error handling with detailed logging

## Error Handling

The solution includes robust error handling for common scenarios:

- **File Not Found**: When the executable doesn't exist
- **Directory Not Found**: When the working directory is invalid
- **Timeout Exceeded**: When execution takes longer than the specified timeout
- **Process Failures**: When the console application returns a non-zero exit code
- **Permission Issues**: When there are insufficient permissions to execute the application

## Monitoring and Observability

### Temporal Web UI

Access the Temporal Web UI at http://localhost:8080 to:

- View all workflow executions
- See detailed execution history
- Monitor current workflow status
- View activity execution details
- Examine error logs and stack traces

### Workflow Queries

The workflow supports several queries:

- `GetStatus()`: Returns current execution status
- `GetResult()`: Returns execution result (if completed)
- `GetAttempts()`: Returns number of execution attempts

### Logging

Comprehensive logging is provided throughout the application:

- Workflow start/completion events
- Activity execution details
- Error conditions and retry attempts
- Performance metrics

## Best Practices

1. **Security**: Only execute trusted console applications and validate input parameters
2. **Timeouts**: Set appropriate timeouts based on expected execution time
3. **Resource Management**: Monitor system resources when running multiple concurrent executions
4. **Error Handling**: Implement proper error handling in your console applications
5. **Logging**: Use structured logging for better observability
6. **Testing**: Test your console applications independently before integrating with Temporal

## Troubleshooting

### Common Issues

1. **Temporal Server Not Running**
   - Ensure Docker containers are running: `docker compose ps`
   - Check Temporal Web UI accessibility: http://localhost:8080

2. **Worker Not Connecting**
   - Verify connection settings in appsettings.json
   - Check network connectivity to Temporal server

3. **Console Application Not Found**
   - Verify the executable path is correct
   - Ensure the file has execute permissions

4. **Timeout Issues**
   - Increase timeout values for long-running applications
   - Check system resources and performance

### Debugging

1. Enable detailed logging by setting log level to "Debug"
2. Use Temporal Web UI to examine workflow execution history
3. Check worker logs for detailed error information
4. Test console applications independently to isolate issues

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Resources

- [Temporal.io Documentation](https://docs.temporal.io/)
- [Temporal .NET SDK](https://github.com/temporalio/sdk-dotnet)
- [Temporal Samples for .NET](https://github.com/temporalio/samples-dotnet)
- [Temporal Community](https://community.temporal.io/)

## Support

For questions or issues:

1. Check the [Temporal Community Forum](https://community.temporal.io/)
2. Review [Temporal Documentation](https://docs.temporal.io/)
3. Open an issue in this repository