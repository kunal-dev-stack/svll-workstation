using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AegisMeshAgent
{
    public class CommandResult
    {
        public string CommandId { get; set; } = string.Empty;
        public string Stdout { get; set; } = string.Empty;
        public string Stderr { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }

    public static class CommandExecutor
    {
        public static async Task<CommandResult> ExecuteAsync(string commandId, string command, string shell = "powershell")
        {
            var result = new CommandResult { CommandId = commandId };

            string fileName = shell.ToLower() == "cmd" ? "cmd.exe" : "powershell.exe";
            string args = shell.ToLower() == "cmd"
                ? $"/c {command}"
                : $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            try
            {
                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                if (await Task.WhenAny(Task.WhenAll(stdoutTask, stderrTask), Task.Delay(30000)) == Task.Delay(30000))
                {
                    process.Kill(true);
                    result.Stderr = "Command timed out after 30 seconds.";
                    result.ExitCode = -1;
                    return result;
                }

                await process.WaitForExitAsync();
                result.Stdout = await stdoutTask;
                result.Stderr = await stderrTask;
                result.ExitCode = process.ExitCode;
            }
            catch (Exception ex)
            {
                result.Stderr = $"Execution error: {ex.Message}";
                result.ExitCode = -1;
            }

            return result;
        }
    }
}
