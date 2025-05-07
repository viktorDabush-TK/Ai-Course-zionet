using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace SemanticKernelPlayground.Plugins
{
    public class GitRepoPlugin
    {
        [KernelFunction]
        public string SelectRepo()
        {
            var repoPath = GetGitRepoRoot();

            if (!string.IsNullOrEmpty(repoPath))
            {
                return repoPath;
            }

            Console.Write("Could not auto-detect a Git repo. Enter path manually: ");
            return Console.ReadLine() ?? "";
        }

        private string? GetGitRepoRoot()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --show-toplevel",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return process.StandardOutput.ReadToEnd().Trim();
                }
            }
            catch
            {
                // Log or silently ignore
            }

            return null;
        }
    }
}
