using Microsoft.SemanticKernel;
using LibGit2Sharp;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins
{
    public class GitLogPlugin
    {
        private readonly GitRepoPlugin _repoHelper = new GitRepoPlugin();

        [KernelFunction, Description("Returns a list of recent Git commit messages from the selected repository.")]
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
    }
}
