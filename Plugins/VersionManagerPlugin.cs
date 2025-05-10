using Microsoft.SemanticKernel;
using Semver;
using System.ComponentModel;
using SemanticKernelPlayground.Plugins.Models;

namespace SemanticKernelPlayground.Plugins
{
    public class VersionManagerPlugin
    {
        private static readonly string? VersionFilePath = Helper.GetVersionFilePath();

        [KernelFunction, Description("Reads the current semantic version from the VERSION file.")]
        public VersionResult GetCurrentVersion()
        {
            if (VersionFilePath == null)
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "Could not locate your Git repository or VERSION file."
                };
            }

            var versionText = File.ReadAllText(VersionFilePath).Trim();

            if (!SemVersion.TryParse(versionText, out var semver))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = $"Invalid version format in VERSION file: '{versionText}'. Expected format: x.y.z",
                    FilePath = VersionFilePath
                };
            }

            return new VersionResult
            {
                Success = true,
                Message = $"Current version is {semver}.",
                CurrentVersion = semver.ToString(),
                FilePath = VersionFilePath
            };
        }

        [KernelFunction, Description("Bumps the semantic version based on the specified level (major, minor, patch).")]
        public VersionResult BumpVersion(
            [Description("The version increment type: 'major', 'minor', or 'patch'.")]
            string level = "patch")
        {
            if (VersionFilePath == null)
            {
                return new VersionResult
                {
                    Success = false,
                    Message = "Could not locate your Git repository or VERSION file."
                };
            }

            var versionText = File.ReadAllText(VersionFilePath).Trim();

            if (!SemVersion.TryParse(versionText, out var currentVersion))
            {
                return new VersionResult
                {
                    Success = false,
                    Message = $"Cannot bump version. Invalid format in VERSION file: '{versionText}'",
                    FilePath = VersionFilePath
                };
            }

            var newVersion = level.ToLower() switch
            {
                "major" => new SemVersion(currentVersion.Major + 1, 0, 0),
                "minor" => new SemVersion(currentVersion.Major, currentVersion.Minor + 1, 0),
                "patch" => new SemVersion(currentVersion.Major, currentVersion.Minor, currentVersion.Patch + 1),
                _ => currentVersion
            };

            File.WriteAllText(VersionFilePath, newVersion.ToString());

            return new VersionResult
            {
                Success = true,
                Message = $"Version updated: {currentVersion} → {newVersion}",
                PreviousVersion = currentVersion.ToString(),
                CurrentVersion = newVersion.ToString(),
                FilePath = VersionFilePath
            };
        }
    }
}
