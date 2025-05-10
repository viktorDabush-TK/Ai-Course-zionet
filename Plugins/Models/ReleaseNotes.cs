using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.Plugins.Models
{
    public class ReleaseNoteResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? RepoPath { get; set; }
        public List<CommitInfo>? Commits { get; set; }
    }
    public class CommitInfo
    {
        public string Message { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
    public class ReleaseNoteRequest
    {
        public int CommitCount { get; set; } = 10;
        public string? Author { get; set; }
        public DateTime? Since { get; set; }
        public bool GroupByDate { get; set; } = false;
    }
    public class CommitFilterOptions
    {
        public int? MaxCount { get; set; }
        public string? AuthorContains { get; set; }
        public DateTime? Since { get; set; }
    }


}
