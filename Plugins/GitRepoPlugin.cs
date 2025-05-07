using Microsoft.SemanticKernel;

namespace SemanticKernelPlayground.Plugins
{
    public class GitRepoPlugin
    {
        [KernelFunction]
        public string SelectRepo()
        {
            Console.Write("Enter path to your local Git repository: ");
            return Console.ReadLine() ?? "";
        }
    }

}
