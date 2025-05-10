using Microsoft.SemanticKernel;
using System.ComponentModel;
using SemanticKernelPlayground.Plugins.Models;
using LibGit2Sharp;
using System.Diagnostics;

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

        [KernelFunction, Description("Creates a new branch from an existing one in the active repository.")]
        public GitRepoResult CreateBranch(
    [Description("The name of the source branch to base the new branch on.")] string sourceBranch,
    [Description("The name of the new branch to create.")] string newBranch)
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

                var source = repo.Branches[sourceBranch] ?? repo.Branches.FirstOrDefault(b => b.FriendlyName == sourceBranch);
                if (source == null)
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = $"Source branch '{sourceBranch}' not found."
                    };
                }

                var newCreated = repo.CreateBranch(newBranch, source.Tip);
                return new GitRepoResult
                {
                    Success = true,
                    Message = $"Created new branch '{newCreated.FriendlyName}' from '{sourceBranch}'.",
                    SelectedRepo = _activeRepoPath
                };
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Failed to create branch: {ex.Message}"
                };
            }
        }


        [KernelFunction, Description("Pushes the current branch using Git CLI, including support for new branches.")]
        public GitRepoResult PushWithGitCli()
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
                string currentBranch = string.Empty;

                var branchProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "rev-parse --abbrev-ref HEAD",
                        WorkingDirectory = _activeRepoPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                branchProcess.Start();
                currentBranch = branchProcess.StandardOutput.ReadToEnd().Trim();
                branchProcess.WaitForExit();

                if (string.IsNullOrEmpty(currentBranch))
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = "Failed to determine the current branch name."
                    };
                }

                var upstreamCheck = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"rev-parse --symbolic-full-name --abbrev-ref {currentBranch}@{{u}}",
                        WorkingDirectory = _activeRepoPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                upstreamCheck.Start();
                var upstreamOutput = upstreamCheck.StandardOutput.ReadToEnd();
                var upstreamError = upstreamCheck.StandardError.ReadToEnd();
                upstreamCheck.WaitForExit();

                bool hasUpstream = upstreamCheck.ExitCode == 0;

                var pushArgs = hasUpstream
                    ? "push"
                    : $"push --set-upstream origin {currentBranch}";

                var pushProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = pushArgs,
                        WorkingDirectory = _activeRepoPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                pushProcess.Start();
                string output = pushProcess.StandardOutput.ReadToEnd();
                string error = pushProcess.StandardError.ReadToEnd();
                pushProcess.WaitForExit();

                return pushProcess.ExitCode == 0
                    ? new GitRepoResult
                    {
                        Success = true,
                        Message = $"Push succeeded.\n{output}",
                        SelectedRepo = _activeRepoPath
                    }
                    : new GitRepoResult
                    {
                        Success = false,
                        Message = $"Push failed.\n{error}"
                    };
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Exception during push: {ex.Message}"
                };
            }
        }


        [KernelFunction, Description("Pulls the latest changes from the origin for the current branch using system Git.")]
        public GitRepoResult PullWithGitCli()
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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "pull",
                        WorkingDirectory = _activeRepoPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return new GitRepoResult
                    {
                        Success = true,
                        Message = "Pull succeeded.",
                        SelectedRepo = _activeRepoPath
                    };
                }
                else
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = $"Pull failed:\n{error}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Exception during pull: {ex.Message}"
                };
            }
        }

        [KernelFunction, Description("Merges the specified branch into the current branch using system Git.")]
        public GitRepoResult MergeBranchWithGitCli(
        [Description("The name of the branch to merge into the current branch.")] string sourceBranch)
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
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"merge {sourceBranch}",
                        WorkingDirectory = _activeRepoPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return new GitRepoResult
                    {
                        Success = true,
                        Message = $"Successfully merged branch '{sourceBranch}' into the current branch.",
                        SelectedRepo = _activeRepoPath
                    };
                }
                else
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = $"Merge failed:\n{error}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Exception during merge: {ex.Message}"
                };
            }
        }


        [KernelFunction, Description("Shows unstaged files and optionally stages and commits them.")]
        public GitRepoResult StageAndCommit(
            [Description("Commit message to use")] string commitMessage,
            [Description("List of file paths to stage. If empty and stageAll is false, nothing will be committed.")]
    List<string>? filePaths = null,
            [Description("If true, stage all unstaged files.")] bool stageAll = false)
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

                var status = repo.RetrieveStatus(new StatusOptions());
                var unstagedFiles = status
                    .Where(s => s.State != FileStatus.Ignored && s.State != FileStatus.Unaltered)
                    .Select(s => s.FilePath)
                    .ToList();

                if (unstagedFiles.Count == 0)
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = "No changes to stage or commit."
                    };
                }

                var filesToStage = stageAll ? unstagedFiles : filePaths ?? new();

                if (filesToStage.Count == 0)
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = "No files provided to stage, and stageAll is false."
                    };
                }

                foreach (var path in filesToStage)
                {
                    Commands.Stage(repo, path);
                }

                var author = repo.Config.BuildSignature(DateTimeOffset.Now) ??
                             new Signature("AI Agent", "ai@example.com", DateTimeOffset.Now);

                if (!repo.Index.Any())
                {
                    return new GitRepoResult
                    {
                        Success = false,
                        Message = "No changes staged after processing."
                    };
                }

                var commit = repo.Commit(commitMessage, author, author);

                return new GitRepoResult
                {
                    Success = true,
                    Message = $"Committed with message: '{commitMessage}'",
                    SelectedRepo = _activeRepoPath
                };
            }
            catch (Exception ex)
            {
                return new GitRepoResult
                {
                    Success = false,
                    Message = $"Failed to stage and commit: {ex.Message}"
                };
            }
        }

        [KernelFunction, Description("Lists all modified, untracked, or staged files in the repo.")]
        public GitRepoResult ListChangedFiles()
        {
            if (string.IsNullOrEmpty(_activeRepoPath))
            {
                return new GitRepoResult { Success = false, Message = "No active Git repository selected." };
            }

            try
            {
                using var repo = new Repository(_activeRepoPath);
                var status = repo.RetrieveStatus();
                var changes = status
                    .Where(s => s.State != FileStatus.Ignored && s.State != FileStatus.Unaltered)
                    .Select(s => $"{s.FilePath} — {s.State}")
                    .ToList();

                return new GitRepoResult
                {
                    Success = true,
                    Message = changes.Count > 0 ? "Changed files:" : "No modified or untracked files found.",
                    RepoPaths = changes
                };
            }
            catch (Exception ex)
            {
                return new GitRepoResult { Success = false, Message = $"Failed to retrieve file changes: {ex.Message}" };
            }
        }


        public string? GetRepoPathInternal() => _activeRepoPath;
    }
}
