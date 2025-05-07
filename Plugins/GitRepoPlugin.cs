using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace SemanticKernelPlayground.Plugins
{
    public class GitRepoPlugin
    {
        private static string? _activeRepoPath = TryAutoDetectRepo();
        private static List<string> _discoveredRepos = new();

        private static string? TryAutoDetectRepo()
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
                // Silent fail
            }

            return null;
        }

        [KernelFunction, Description("Scans a base folder for Git repositories and lists them.")]
        public string ListGitRepos(string basePath)
        {
            if (!Directory.Exists(basePath))
                return $"Path '{basePath}' does not exist.";

            _discoveredRepos = Directory
                .EnumerateDirectories(basePath, "*", SearchOption.AllDirectories)
                .Where(dir => Directory.Exists(Path.Combine(dir, ".git")))
                .ToList();

            if (_discoveredRepos.Count == 0)
                return $"No Git repositories found in '{basePath}'.";

            var result = "Discovered Git Repositories:\n";
            for (int i = 0; i < _discoveredRepos.Count; i++)
            {
                result += $"{i + 1}. {_discoveredRepos[i]}\n";
            }

            return result;
        }

        [KernelFunction, Description("Selects a Git repository from the discovered list.")]
        public string SelectGitRepoByIndex(int index)
        {
            if (_discoveredRepos == null || _discoveredRepos.Count == 0)
                return "No repositories have been discovered. Use ListGitRepos first.";

            if (index < 1 || index > _discoveredRepos.Count)
                return $"Invalid index. Please choose between 1 and {_discoveredRepos.Count}.";

            _activeRepoPath = _discoveredRepos[index - 1];
            return $"Selected repository: {_activeRepoPath}";
        }

        [KernelFunction, Description("Sets the active Git repository path manually.")]
        public string SetActiveRepoPath(string repoPath)
        {
            if (!Directory.Exists(Path.Combine(repoPath, ".git")))
                return $"'{repoPath}' is not a valid Git repository.";

            _activeRepoPath = repoPath;
            return $"Active Git repository set to: {_activeRepoPath}";
        }

        [KernelFunction, Description("Returns the currently selected Git repository path.")]
        public string GetActiveRepoPath() =>
            _activeRepoPath ?? "No Git repository is currently selected.";

        public string? GetRepoPathInternal() => _activeRepoPath;
    }
}
