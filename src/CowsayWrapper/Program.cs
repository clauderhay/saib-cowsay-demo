using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CowsayWrapper
{
    class Program
    {
        private const string COWSAY_PATH = "/opt/homebrew/bin/cowsay";
        private const int PROCESS_TIMEOUT_MS = 5000;

        static async Task Main()
        {
            Console.WriteLine("=== Cowsay Program ===");

            if (!ValidateEnvironment())
                return;

            await RunCowsayService();
        }

        private static bool ValidateEnvironment()
        {
            if (!File.Exists(COWSAY_PATH))
            {
                WriteError($"Cowsay not found at: {COWSAY_PATH}");
                return false;
            }

            return true;
        }

        private static async Task RunCowsayService()
        {
            while (true)
            {
                Console.Write("\nEnter message (or 'quit' to exit): ");
                var input = Console.ReadLine();

                if (string.IsNullOrEmpty(input) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                    break;

                var result = await ExecuteCowsay(input);
                DisplayResult(result);
            }

            Console.WriteLine("Goodbye!");
        }

        /// <summary>
        /// Executes cowsay using STDIN communication instead of command-line arguments.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task<ProcessResult> ExecuteCowsay(string message)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = COWSAY_PATH,
                // No arguments, cowsay will read from STDIN
                UseShellExecute = false,
                RedirectStandardInput = true,     // Enable writing TO the process
                RedirectStandardOutput = true,    // Enable reading FROM the process  
                RedirectStandardError = true,     // Capture error stream separately
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };

            try
            {
                if (!process.Start())
                    return ProcessResult.Failure("Failed to start cowsay process");

                // Concurrently handle I/O to prevent deadlocks
                var communicationTask = CommunicateWithProcess(process, message);
                var timeoutTask = Task.Delay(PROCESS_TIMEOUT_MS);

                var completedTask = await Task.WhenAny(communicationTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    KillProcess(process);
                    return ProcessResult.Failure($"Process timed out after {PROCESS_TIMEOUT_MS}ms");
                }

                return await communicationTask;
            }
            catch (Exception ex)
            {
                return ProcessResult.Failure($"Process execution error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles STDIN writing and STDOUT/STDERR reading concurrently to avoid deadlocks.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task<ProcessResult> CommunicateWithProcess(Process process, string message)
        {
            // Start all I/O operations concurrently
            var stdinTask = WriteToStdin(process, message);
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var exitTask = process.WaitForExitAsync();

            // Wait for all operations to complete
            await Task.WhenAll(stdinTask, stdoutTask, stderrTask, exitTask);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            var exitCode = process.ExitCode;

            // Analyze results with proper error handling
            if (exitCode != 0)
            {
                var errorMessage = !string.IsNullOrEmpty(stderr)
                    ? $"Process failed (exit code {exitCode}): {stderr.Trim()}"
                    : $"Process failed with exit code {exitCode}";
                return ProcessResult.Failure(errorMessage);
            }

            if (string.IsNullOrEmpty(stdout))
            {
                return ProcessResult.Failure("Process completed but produced no output");
            }

            return ProcessResult.Success(stdout, exitCode);
        }

        /// <summary>
        /// Safely writes message to process STDIN and closes the stream.
        /// </summary>
        /// <param name="process"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task WriteToStdin(Process process, string message)
        {
            try
            {
                await process.StandardInput.WriteLineAsync(message);
                process.StandardInput.Close(); // Signal EOF to cowsay
            }
            catch (Exception ex)
            {
                // Log but don't throw; let main communication handler deal with it
                WriteError($"STDIN write error: {ex.Message}");
            }
        }

        private static void KillProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                WriteError($"Failed to kill process: {ex.Message}");
            }
        }

        private static void DisplayResult(ProcessResult result)
        {
            Console.WriteLine();

            if (result.IsSuccess)
            {
                Console.WriteLine("--- Cowsay Output ---");
                Console.WriteLine(result.Output);
                Console.WriteLine($"Process completed successfully (exit code: {result.ExitCode})");
            }
            else
            {
                WriteError($"Error: {result.ErrorMessage}");
            }
        }

        private static void WriteError(string message)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// Represents the result of a process execution with proper success/failure     
    /// </summary>
    public class ProcessResult
    {
        public bool IsSuccess { get; private set; }
        public string Output { get; private set; } = string.Empty;
        public string ErrorMessage { get; private set; } = string.Empty;
        public int ExitCode { get; private set; }

        private ProcessResult() { }

        public static ProcessResult Success(string output, int exitCode) => new ProcessResult
        {
            IsSuccess = true,
            Output = output,
            ExitCode = exitCode
        };

        public static ProcessResult Failure(string errorMessage) => new ProcessResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ExitCode = -1
        };
    }
}