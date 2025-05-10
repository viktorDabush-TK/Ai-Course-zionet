using Microsoft.SemanticKernel;
using Semver;
using System.ComponentModel;

namespace SemanticKernelPlayground.Plugins
{
    public class VersionManagerPlugin
    {
        private static readonly string? VersionFilePath = Helper.GetVersionFilePath();

        [KernelFunction, Description("Reads the current semantic version from the VERSION file.")]
        public string GetCurrentVersion()
        {
            if (VersionFilePath == null)
            {
                return "Could not locate your Git repository or VERSION file.";
            }

            var versionText = File.ReadAllText(VersionFilePath).Trim();

            if (!SemVersion.TryParse(versionText, out var semver))
            {
                return $"Invalid version format in VERSION file: '{versionText}'. Expected format: x.y.z";
            }

            return $"Current version is {semver}.";
        }

        [KernelFunction, Description("Bumps the semantic version based on the specified level (major, minor, patch).")]
        public string BumpVersion(
            [Description("The version increment type: 'major', 'minor', or 'patch'.")]
            string level = "patch")
        {
            if (VersionFilePath == null)
            {
                return "Could not locate your Git repository or VERSION file.";
            }

            var versionText = File.ReadAllText(VersionFilePath).Trim();

            if (!SemVersion.TryParse(versionText, out var currentVersion))
            {
                return $"Cannot bump version. Invalid format in VERSION file: '{versionText}'";
            }

            SemVersion newVersion = level.ToLower() switch
            {
                "major" => new SemVersion(currentVersion.Major + 1, 0, 0),
                "minor" => new SemVersion(currentVersion.Major, currentVersion.Minor + 1, 0),
                "patch" => new SemVersion(currentVersion.Major, currentVersion.Minor, currentVersion.Patch + 1),
                _ => currentVersion
            };

            File.WriteAllText(VersionFilePath, newVersion.ToString());
            return $"Version updated: {currentVersion} → {newVersion}";
        }
    }
}
