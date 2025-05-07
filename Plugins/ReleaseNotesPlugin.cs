using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace SemanticKernelPlayground.Plugins
{
    public class ReleaseNotesPlugin
    {
        [KernelFunction]
        public string GenerateReleaseNotes(List<string> commits)
        {
            return $"Release Notes:\n- {string.Join("\n- ", commits)}";
        }
    }

}
