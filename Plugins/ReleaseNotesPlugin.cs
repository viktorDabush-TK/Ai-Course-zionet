using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using LibGit2Sharp;
using System.ComponentModel;
using System.Text;

namespace SemanticKernelPlayground.Plugins
{
    public class ReleaseNotesPlugin
    {
        private readonly Kernel _kernel;
        private readonly GitRepoPlugin _repoHelper = new GitRepoPlugin();

        public ReleaseNotesPlugin(Kernel kernel)
        {
            _kernel = kernel;
        }

        [KernelFunction, Description("Returns the raw commit messages from the selected Git repository.")]
        public List<string> ListCommits(
            [Description("Number of commits to list. Defaults to 10.")] int commitCount = 10)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();

            if (string.IsNullOrEmpty(repoPath))
            {
                return new List<string>
                {
                    "No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first."
                };
            }

            using var repo = new Repository(repoPath);

            return repo.Commits
                       .Take(commitCount)
                       .Select(c => c.MessageShort)
                       .ToList();
        }

        [KernelFunction, Description("Generates professional release notes from recent Git commits using AI (preview only).")]
        public async Task<string> GenerateReleaseNotesAsync(
            [Description("Number of recent commits to include. Defaults to 10.")] int commitCount = 10)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();

            if (string.IsNullOrEmpty(repoPath))
            {
                return "No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first.";
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

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var history = new ChatHistory();
            history.AddUserMessage($"""
                You are a professional release manager. Summarize the following Git commits into a clean and well-structured markdown release note.

                Commits:
                {string.Join("\n", commits)}

                Format the output using markdown with sections like:
                - ## Features
                - ## Bug Fixes
                - ## Improvements

                Only include sections that apply. Be concise and professional.
            """);

            var response = await chatService.GetChatMessageContentAsync(history, kernel: _kernel);

            return response?.Content ?? "Failed to generate release notes.";
        }

        [KernelFunction, Description("Generates release notes and saves them to RELEASE_NOTES.md in the selected Git repository.")]
        public async Task<string> GenerateAndSaveReleaseNotesAsync(
            [Description("Number of recent commits to include. Defaults to 10.")] int commitCount = 10)
        {
            var notes = await GenerateReleaseNotesAsync(commitCount);

            if (notes.StartsWith("No Git repository selected.") || notes.StartsWith("No commits found") || notes.StartsWith("Failed to"))
            {
                return notes;
            }

            var repoPath = _repoHelper.GetRepoPathInternal();
            if (string.IsNullOrEmpty(repoPath))
            {
                return "No Git repository selected.";
            }

            var filePath = Path.Combine(repoPath, "RELEASE_NOTES.md");

            File.WriteAllText(filePath, notes, Encoding.UTF8);
            return $"Release notes generated and saved to:\n{filePath}";
        }
    }
}
