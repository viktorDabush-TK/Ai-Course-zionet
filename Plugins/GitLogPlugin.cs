using Microsoft.SemanticKernel;
using LibGit2Sharp;
namespace SemanticKernelPlayground.Plugins
{
    public class GitLogPlugin
    {
        [KernelFunction]
        public List<string> GetCommits(string repoPath)
        {
            using var repo = new Repository(repoPath);
            return repo.Commits.Select(c => c.MessageShort).ToList();
        }
    }
}
