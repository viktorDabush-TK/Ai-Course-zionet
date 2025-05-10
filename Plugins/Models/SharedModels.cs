namespace SemanticKernelPlayground.Plugins.Models
{
    public class CommitInfo
    {
        public string Message { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
    public class CommitSummaryResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RepoPath { get; set; }
        public List<CommitInfo> Commits { get; set; } = new();
    }
}
