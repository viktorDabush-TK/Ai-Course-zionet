using Microsoft.SemanticKernel;
using System.ComponentModel;
using SemanticKernelPlayground.Plugins.Models;
using LibGit2Sharp;

namespace SemanticKernelPlayground.Plugins
{
    public class GitRepoPlugin
    {
        private static string? _activeRepoPath = Helper.TryAutoDetectRepo()?.Path;
        private static List<string> _discoveredRepos = new();
        private static List<string> _skippedPaths = new();

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

            var (repos, skipped) = Helper.SafeEnumerateGitRepos(basePath);
            _discoveredRepos = repos;
            _skippedPaths = skipped;

            if (_discoveredRepos.Count == 0)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"No Git repositories found in '{basePath}'.",
                    Warnings = _skippedPaths
                };
            }

            return new GitRepoResult
            {
                Success = true,
                Message = $"Found {_discoveredRepos.Count} repositories.",
                RepoPaths = _discoveredRepos,
                Warnings = _skippedPaths
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

        [KernelFunction, Description("Lists all local branches in the active Git repository.")]
        public GitRepoResult ListBranches()
        {
            if (string.IsNullOrEmpty(_activeRepoPath))
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = "No active Git repository selected."
                };
            }

            try
            {
                using var repo = new Repository(_activeRepoPath);
                var branches = repo.Branches
                    .Where(b => !b.IsRemote)
                    .Select(b => b.FriendlyName)
                    .ToList();

                return new GitRepoResult
                {
                    Success = true,
                    Message = $"Found {branches.Count} local branches.",
                    RepoPaths = branches
                };
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Failed to list branches: {ex.Message}"
                };
            }
        }

        [KernelFunction, Description("Switches to the specified Git branch in the active repository.")]
        public GitRepoResult CheckoutBranch(string branchName)
        {
            if (string.IsNullOrEmpty(_activeRepoPath))
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = "No active Git repository selected."
                };
            }

            try
            {
                using var repo = new Repository(_activeRepoPath);
                var branch = repo.Branches[branchName] ?? repo.Branches.FirstOrDefault(b => b.FriendlyName == branchName);

                if (branch == null)
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = $"Branch '{branchName}' not found."
                    };
                }

                Commands.Checkout(repo, branch);

                return new GitRepoResult
                {
                    Success = true,
                    Message = $"Switched to branch '{branch.FriendlyName}'.",
                    SelectedRepo = _activeRepoPath
                };
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Failed to switch branch: {ex.Message}"
                };
            }
        }

        [KernelFunction, Description("Returns the name of the currently checked-out Git branch.")]
        public GitRepoResult GetCurrentBranch()
        {
            if (string.IsNullOrEmpty(_activeRepoPath))
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = "No active Git repository selected."
                };
            }

            try
            {
                using var repo = new Repository(_activeRepoPath);
                var currentBranch = repo.Head?.FriendlyName ?? "(unknown)";

                return new GitRepoResult
                {
                    Success = true,
                    Message = $"You are currently on branch '{currentBranch}'.",
                    SelectedRepo = _activeRepoPath
                };
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Failed to get current branch: {ex.Message}"
                };
            }
        }

        public string? GetRepoPathInternal() => _activeRepoPath;
    }
}
