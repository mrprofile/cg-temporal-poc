using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PythonInvocation
{
    public class BasicPythonExecutor
    {
        private readonly string _pythonExecutablePath;
        
        public BasicPythonExecutor(string pythonPath = "python")
        {
            _pythonExecutablePath = pythonPath;
        }
        
        /// <summary>
        /// Executes a Python script with arguments and captures output
        /// </summary>
        /// <param name="scriptPath">Path to the Python script</param>
        /// <param name="arguments">Command line arguments for the script</param>
        /// <returns>Execution result containing output, errors, and exit code</returns>
        public async Task<PythonExecutionResult> ExecutePythonScriptAsync(string scriptPath, params string[] arguments)
        {
            try
            {
                // Validate script exists
                if (!File.Exists(scriptPath))
                {
                    throw new FileNotFoundException($"Python script not found: {scriptPath}");
                }
                
                // Build argument string
                var argString = string.Join(" ", arguments.Select(arg => $"\"{arg}\""));
                var fullCommand = $"\"{scriptPath}\" {argString}";
                
                // Configure process start info
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutablePath,
                    Arguments = fullCommand,
                    UseShellExecute = false,           // Required for redirection
                    RedirectStandardOutput = true,     // Capture stdout
                    RedirectStandardError = true,      // Capture stderr
                    RedirectStandardInput = true,      // Allow input if needed
                    CreateNoWindow = true,             // Don't show console window
                    WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Environment.CurrentDirectory
                };
                
                // Add environment variables if needed
                processStartInfo.EnvironmentVariables["PYTHONPATH"] = Environment.CurrentDirectory;
                processStartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1"; // Force unbuffered output
                
                using var process = new Process { StartInfo = processStartInfo };
                
                // Start the process
                var startTime = DateTime.UtcNow;
                process.Start();
                
                // Read output and error streams asynchronously
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                
                // Wait for process completion with timeout
                var timeoutMs = 30000; // 30 seconds
                var processExited = await Task.Run(() => process.WaitForExit(timeoutMs));
                
                if (!processExited)
                {
                    process.Kill();
                    throw new TimeoutException($"Python script execution timed out after {timeoutMs}ms");
                }
                
                // Collect results
                var output = await outputTask;
                var error = await errorTask;
                var executionTime = DateTime.UtcNow - startTime;
                
                return new PythonExecutionResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = output,
                    StandardError = error,
                    ExecutionTime = executionTime,
                    Success = process.ExitCode == 0
                };
            }
            catch (Exception ex)
            {
                return new PythonExecutionResult
                {
                    ExitCode = -1,
                    StandardOutput = string.Empty,
                    StandardError = ex.Message,
                    ExecutionTime = TimeSpan.Zero,
                    Success = false
                };
            }
        }
        
        /// <summary>
        /// Synchronous version of Python script execution
        /// </summary>
        public PythonExecutionResult ExecutePythonScript(string scriptPath, params string[] arguments)
        {
            return ExecutePythonScriptAsync(scriptPath, arguments).GetAwaiter().GetResult();
        }
    }
    
    /// <summary>
    /// Result object containing Python script execution details
    /// </summary>
    public class PythonExecutionResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public TimeSpan ExecutionTime { get; set; }
        public bool Success { get; set; }
        
        public override string ToString()
        {
            return $"ExitCode: {ExitCode}, Success: {Success}, ExecutionTime: {ExecutionTime.TotalMilliseconds}ms";
        }
    }
}