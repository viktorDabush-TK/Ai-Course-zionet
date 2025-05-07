using Microsoft.SemanticKernel;
using LibGit2Sharp;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins
{
    public class ReleaseNotesPlugin
    {
        private readonly GitRepoPlugin _repoHelper = new GitRepoPlugin();

        [KernelFunction, Description("Generates release notes from recent Git commit messages.")]
        public string GenerateReleaseNotes(
            [Description("The number of recent commits to include in the release notes. Defaults to 10.")]
            int commitCount = 10)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();

            if (string.IsNullOrEmpty(repoPath))
            {
                return "⚠️ No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first.";
            }

            using var repo = new Repository(repoPath);

            var commits = repo.Commits
                              .Take(commitCount)
                              .Select(c => c.MessageShort)
                              .ToList();

            if (commits.Count == 0)
            {
                return "No commits found in the selected repository.";
            }

            return $"📦 Release Notes (Last {commitCount} Commits):\n- {string.Join("\n- ", commits)}";
        }
    }
}
