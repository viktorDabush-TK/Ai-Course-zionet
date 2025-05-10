using Microsoft.SemanticKernel;
using System.ComponentModel;
using SemanticKernelPlayground.Plugins.Models;

namespace SemanticKernelPlayground.Plugins
{
    public class GitRepoPlugin
    {
        private static string? _activeRepoPath = Helper.TryAutoDetectRepo();
        private static List<string> _discoveredRepos = new();

        [KernelFunction, Description("Scans a base folder for Git repositories and lists them.")]
        public GitRepoResult ListGitRepos(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Path '{basePath}' does not exist."
                };
            }

            _discoveredRepos = Directory
                .EnumerateDirectories(basePath, "*", SearchOption.AllDirectories)
                .Where(dir => Directory.Exists(Path.Combine(dir, ".git")))
                .ToList();

            if (_discoveredRepos.Count == 0)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"No Git repositories found in '{basePath}'."
                };
            }

            return new GitRepoResult
            {
                Success = true,
                Message = $"Found {_discoveredRepos.Count} repositories.",
                RepoPaths = _discoveredRepos
            };
        }

        [KernelFunction, Description("Selects a Git repository from the discovered list.")]
        public GitRepoResult SelectGitRepoByIndex(int index)
        {
            if (_discoveredRepos == null || _discoveredRepos.Count == 0)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = "No repositories have been discovered. Use ListGitRepos first."
                };
            }

            if (index < 1 || index > _discoveredRepos.Count)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Invalid index. Please choose between 1 and {_discoveredRepos.Count}."
                };
            }

            _activeRepoPath = _discoveredRepos[index - 1];

            return new GitRepoResult
            {
                Success = true,
                Message = $"Selected repository: {_activeRepoPath}",
                SelectedRepo = _activeRepoPath
            };
        }

        [KernelFunction, Description("Sets the active Git repository path manually.")]
        public GitRepoResult SetActiveRepoPath(string repoPath)
        {
            if (!Directory.Exists(Path.Combine(repoPath, ".git")))
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"'{repoPath}' is not a valid Git repository."
                };
            }

            _activeRepoPath = repoPath;

            return new GitRepoResult
            {
                Success = true,
                Message = $"Active Git repository set to: {_activeRepoPath}",
                SelectedRepo = _activeRepoPath
            };
        }

        [KernelFunction, Description("Returns the currently selected Git repository path.")]
        public GitRepoResult GetActiveRepoPath()
        {
            if (string.IsNullOrEmpty(_activeRepoPath))
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = "No Git repository is currently selected."
                };
            }

            return new GitRepoResult
            {
                Success = true,
                Message = $"Active Git repository: {_activeRepoPath}",
                SelectedRepo = _activeRepoPath
            };
        }

        public string? GetRepoPathInternal() => _activeRepoPath;
    }
}
