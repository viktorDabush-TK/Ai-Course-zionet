using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.Plugins.Models
{
    public class GitRepoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public List<string>? RepoPaths { get; set; }

        public List<GitFileChange>? ChangedFiles { get; set; }

        public string? SelectedRepo { get; set; }
        public List<string>? Warnings { get; set; }
    }
    public class GitFileChange
    {
        public string FilePath { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
    }
}
