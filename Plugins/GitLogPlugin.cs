using Microsoft.SemanticKernel;
using LibGit2Sharp;
using System.ComponentModel;
using SemanticKernelPlayground.Plugins.Models;

namespace SemanticKernelPlayground.Plugins
{
    public class GitLogPlugin
    {
        private readonly GitRepoPlugin _repoHelper = new GitRepoPlugin();

        [KernelFunction, Description("Returns recent commit objects from the selected repository.")]
        public List<CommitInfo> GetCommits(
            [Description("The number of recent commits to return. Defaults to 10.")] int commitCount = 10)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();

            if (string.IsNullOrEmpty(repoPath))
            {
                return new List<CommitInfo>
                {
                    new CommitInfo { Message = "No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first." }
                };
            }

            using var repo = new Repository(repoPath);

            return repo.Commits
                       .Take(commitCount)
                       .Select(c => new CommitInfo
                       {
                           Message = c.MessageShort,
                           Author = c.Author.Name,
                           Date = c.Committer.When.LocalDateTime
                       })
                       .ToList();
        }

        [KernelFunction, Description("Returns a formatted summary of recent commits for chat or display.")]
        public CommitSummaryResult GetCommitSummary(
            [Description("The number of commits to summarize. Defaults to 10.")] int commitCount = 10)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();

            if (string.IsNullOrEmpty(repoPath))
            {
                return new CommitSummaryResult
                {
                    Success = false,
                    Message = "No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first."
                };
            }

            using var repo = new Repository(repoPath);

            var commits = repo.Commits
                              .Take(commitCount)
                              .Select(c => new CommitInfo
                              {
                                  Message = c.MessageShort,
                                  Author = c.Author.Name,
                                  Date = c.Committer.When.LocalDateTime
                              })
                              .ToList();

            if (commits.Count == 0)
            {
                return new CommitSummaryResult
                {
                    Success = false,
                    RepoPath = repoPath,
                    Message = "No commits found in the selected repository."
                };
            }

            var formatted = string.Join("\n", commits.Select(c =>
                $"- {c.Author} ({c.Date:yyyy-MM-dd}): {c.Message}"));

            return new CommitSummaryResult
            {
                Success = true,
                RepoPath = repoPath,
                Commits = commits,
                Message = $"Last {commitCount} commits:\n{formatted}"
            };
        }
    }
}
