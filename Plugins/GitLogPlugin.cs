using Microsoft.SemanticKernel;
using LibGit2Sharp;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins
{
    public class GitLogPlugin
    {
        private readonly GitRepoPlugin _repoHelper = new GitRepoPlugin();

        [KernelFunction, Description("Returns raw commit messages from the selected repository.")]
        public List<string> GetCommits(
            [Description("The number of recent commits to return. Defaults to 10.")] int commitCount = 10)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();

            if (string.IsNullOrEmpty(repoPath))
            {
                return new List<string> {
                    "⚠️ No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first."
                };
            }

            using var repo = new Repository(repoPath);

            return repo.Commits
                       .Take(commitCount)
                       .Select(c => c.MessageShort)
                       .ToList();
        }

        [KernelFunction, Description("Returns a formatted summary of recent commits for chat or display.")]
        public string GetCommitSummary(
            [Description("The number of commits to summarize. Defaults to 10.")] int commitCount = 10)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();

            if (string.IsNullOrEmpty(repoPath))
            {
                return "⚠️ No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first.";
            }

            using var repo = new Repository(repoPath);

            var commits = repo.Commits
                              .Take(commitCount)
                              .Select(c => $"- {c.Author.Name} ({c.Committer.When.LocalDateTime:yyyy-MM-dd}): {c.MessageShort}")
                              .ToList();

            if (commits.Count == 0)
            {
                return "No commits found in the selected repository.";
            }

            return $"📝 Last {commitCount} commits:\n{string.Join("\n", commits)}";
        }
    }
}
