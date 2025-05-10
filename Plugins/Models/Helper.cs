using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.Plugins.Models
{
    public class GitDetectionResult
    {
        public bool Success { get; set; }
        public string? Path { get; set; }
        public string? Message { get; set; }
    }
}
