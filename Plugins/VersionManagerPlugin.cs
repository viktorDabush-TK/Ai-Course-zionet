using Microsoft.SemanticKernel;
using Semver;
using System.ComponentModel;
using SemanticKernelPlayground.Plugins.Models;

namespace SemanticKernelPlayground.Plugins
{
    public class VersionManagerPlugin
    {
        [KernelFunction, Description("Reads the current semantic version from the VERSION file.")]
        public VersionResult GetCurrentVersion()
        {
            var repoRoot = Helper.TryAutoDetectRepo()?.Path;
            if (repoRoot == null)
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "Could not locate your Git repository."
                };
            }

            var versionPath = Path.Combine(repoRoot, "VERSION");

            if (!File.Exists(versionPath))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "VERSION file does not exist. Use `CreateVersionFile` to create it.",
                    FilePath = versionPath
                };
            }

            var versionText = File.ReadAllText(versionPath).Trim();

            if (!SemVersion.TryParse(versionText, out var semver))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = $"Invalid version format in VERSION file: '{versionText}'. Expected format: x.y.z",
                    FilePath = versionPath
                };
            }

            return new VersionResult
            {
                Success = true,
                Message = $"Current version is {semver}.",
                CurrentVersion = semver.ToString(),
                FilePath = versionPath
            };
        }

        [KernelFunction, Description("Creates a VERSION file with a specified version. Default is 0.0.0.")]
        public VersionResult CreateVersionFile(
            [Description("Initial version to write (e.g., 1.0.0). Defaults to 0.0.0.")] string initialVersion = "0.0.0")
        {
            var repoRoot = Helper.TryAutoDetectRepo()?.Path;
            if (repoRoot == null)
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "Could not locate your Git repository."
                };
            }

            var versionPath = Path.Combine(repoRoot, "VERSION");

            if (File.Exists(versionPath))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "VERSION file already exists.",
                    FilePath = versionPath
                };
            }

            if (!SemVersion.TryParse(initialVersion, out var semver))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = $"Invalid version format: '{initialVersion}'. Expected format: x.y.z"
                };
            }

            File.WriteAllText(versionPath, semver.ToString());

            return new VersionResult
            {
                Success = true,
                Message = $"VERSION file created with version {semver}.",
                CurrentVersion = semver.ToString(),
                FilePath = versionPath
            };
        }

        [KernelFunction, Description("Bumps the semantic version based on the specified level (major, minor, patch).")]
        public VersionResult BumpVersion(
            [Description("The version increment type: 'major', 'minor', or 'patch'.")]
            string level = "patch")
        {
            var repoRoot = Helper.TryAutoDetectRepo()?.Path;
            if (repoRoot == null)
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "Could not locate your Git repository."
                };
            }

            var versionPath = Path.Combine(repoRoot, "VERSION");

            if (!File.Exists(versionPath))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "VERSION file does not exist. Use `CreateVersionFile` first.",
                    FilePath = versionPath
                };
            }

            var versionText = File.ReadAllText(versionPath).Trim();

            if (!SemVersion.TryParse(versionText, out var currentVersion))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = $"Cannot bump version. Invalid format in VERSION file: '{versionText}'",
                    FilePath = versionPath
                };
            }

            var newVersion = level.ToLower() switch
            {
                "major" => new SemVersion(currentVersion.Major + 1, 0, 0),
                "minor" => new SemVersion(currentVersion.Major, currentVersion.Minor + 1, 0),
                "patch" => new SemVersion(currentVersion.Major, currentVersion.Minor, currentVersion.Patch + 1),
                _ => currentVersion
            };

            File.WriteAllText(versionPath, newVersion.ToString());

            return new VersionResult
            {
                Success = true,
                Message = $"Version updated: {currentVersion} → {newVersion}",
                PreviousVersion = currentVersion.ToString(),
                CurrentVersion = newVersion.ToString(),
                FilePath = versionPath
            };
        }
    }
}
