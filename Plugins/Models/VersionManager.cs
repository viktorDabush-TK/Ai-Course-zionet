using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.Plugins.Models
{
    public class VersionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CurrentVersion { get; set; }
        public string? PreviousVersion { get; set; }
        public string? FilePath { get; set; }
    }
}
