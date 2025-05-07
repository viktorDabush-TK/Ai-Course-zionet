using Microsoft.SemanticKernel;
using LibGit2Sharp;

namespace SemanticKernelPlayground.Plugins
{
    public class GitLogPlugin
    {
        private readonly GitRepoPlugin _repoHelper = new GitRepoPlugin();

        [KernelFunction]
        public List<string> GetCommits()
        {
            var repoPath = _repoHelper.SelectRepo();
            using var repo = new Repository(repoPath);
            return repo.Commits.Select(c => c.MessageShort).ToList();
        }
    }
}
