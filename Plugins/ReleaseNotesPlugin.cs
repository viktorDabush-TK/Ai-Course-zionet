using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using LibGit2Sharp;
using System.ComponentModel;
using System.Text;
using SemanticKernelPlayground.Plugins.Models;

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

        [KernelFunction, Description("Returns commit details from the selected Git repository.")]
        public List<CommitInfo> ListCommits(
            [Description("Number of commits to list. Defaults to 10.")] int commitCount = 10)
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

        [KernelFunction, Description("Generates professional release notes from recent Git commits using AI (preview only).")]
        public async Task<ReleaseNoteResult> GenerateReleaseNotesAsync(ReleaseNoteRequest request)
        {
            var repoPath = _repoHelper.GetRepoPathInternal();
            if (string.IsNullOrEmpty(repoPath))
            {
                return new ReleaseNoteResult
                {
                    Success = false,
                    Message = "No Git repository selected. Use 'SetActiveRepoPath' or 'SelectGitRepoByIndex' first."
                };
            }

            using var repo = new Repository(repoPath);

            var commits = GetFilteredCommits(repo, new CommitFilterOptions
            {
                MaxCount = request.CommitCount,
                AuthorContains = request.Author,
                Since = request.Since
            });

            if (commits.Count == 0)
            {
                return new ReleaseNoteResult
                {
                    Success = false,
                    Message = "No matching commits found.",
                    RepoPath = repoPath
                };
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var history = new ChatHistory();
            history.AddUserMessage($"""
                You are a professional release manager. Summarize the following Git commits into a clean and well-structured markdown release note.

                Commits:
                {string.Join("\n", commits.Select(c => $"- {c.Message}"))}

                Format the output using markdown with sections like:
                - ## Features
                - ## Bug Fixes
                - ## Improvements

                Only include sections that apply. Be concise and professional.
            """);

            var response = await chatService.GetChatMessageContentAsync(history, kernel: _kernel);

            if (response == null || string.IsNullOrWhiteSpace(response.Content))
            {
                return new ReleaseNoteResult
                {
                    Success = false,
                    Message = "Failed to generate release notes.",
                    RepoPath = repoPath
                };
            }

            return new ReleaseNoteResult
            {
                Success = true,
                Message = response.Content,
                RepoPath = repoPath,
                Commits = commits
            };
        }

        [KernelFunction, Description("Generates release notes and saves them to RELEASE_NOTES.md in the selected Git repository.")]
        public async Task<ReleaseNoteResult> GenerateAndSaveReleaseNotesAsync(ReleaseNoteRequest request)
        {
            var result = await GenerateReleaseNotesAsync(request);

            if (!result.Success || string.IsNullOrWhiteSpace(result.Message) || string.IsNullOrWhiteSpace(result.RepoPath))
            {
                return result;
            }

            var filePath = Path.Combine(result.RepoPath, "RELEASE_NOTES.md");
            File.WriteAllText(filePath, result.Message, Encoding.UTF8);

            result.FilePath = filePath;
            result.Message = "Release notes generated and saved.";
            return result;
        }

        private List<CommitInfo> GetFilteredCommits(Repository repo, CommitFilterOptions options)
        {
            var commits = repo.Commits.AsQueryable();

            if (options.Since.HasValue)
            {
                commits = commits.Where(c => c.Committer.When.UtcDateTime >= options.Since.Value);
            }

            if (!string.IsNullOrWhiteSpace(options.AuthorContains))
            {
                commits = commits.Where(c => c.Author.Name.Contains(options.AuthorContains, StringComparison.OrdinalIgnoreCase));
            }

            if (options.MaxCount.HasValue)
            {
                commits = commits.Take(options.MaxCount.Value);
            }

            return commits
                .Select(c => new CommitInfo
                {
                    Message = c.MessageShort,
                    Author = c.Author.Name,
                    Date = c.Committer.When.LocalDateTime
                })
                .ToList();
        }
    }
}
