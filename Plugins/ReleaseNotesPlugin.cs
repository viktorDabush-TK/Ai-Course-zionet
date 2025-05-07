using Microsoft.SemanticKernel;
using LibGit2Sharp;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins
{
    public class ReleaseNotesPlugin
    {
        private readonly GitRepoPlugin _repoHelper = new GitRepoPlugin();

        [KernelFunction, Description("Generates release notes from recent Git commits.")]
        public string GenerateReleaseNotes(
            [Description("How many recent commits to include in the release notes.")] int commitCount = 10)


        {
            var repoPath = _repoHelper.SelectRepo();
            using var repo = new Repository(repoPath);

            var commits = repo.Commits
                .Take(commitCount)
                .Select(c => c.MessageShort)
                .ToList();

            return $"Release Notes (Last {commitCount} Commits):\n- {string.Join("\n- ", commits)}";
        }
    }
}
