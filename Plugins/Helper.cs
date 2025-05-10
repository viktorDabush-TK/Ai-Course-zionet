using System.Diagnostics;
using SemanticKernelPlayground.Plugins.Models;

namespace SemanticKernelPlayground.Plugins
{
    public static class Helper
    {
        public static string? GetVersionFilePath(bool createIfMissing = true)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    var versionPath = Path.Combine(dir.FullName, "VERSION");

                    if (!File.Exists(versionPath))
                    {
                        if (createIfMissing)
                        {
                            File.WriteAllText(versionPath, "0.0.0");
                        }
                        else
                        {
                            return null;
                        }
                    }

                    return versionPath;
                }

                dir = dir.Parent;
            }

            return null;
        }

        public static GitDetectionResult TryAutoDetectRepo()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --show-toplevel",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return new GitDetectionResult
                    {
                        Success = true,
                        Path = process.StandardOutput.ReadToEnd().Trim()
                    };
                }
            }
            catch (Exception ex)
            {
                return new GitDetectionResult
                {
                    Success = false,
                    Message = $"Exception while detecting Git repo: {ex.Message}"
                };
            }

            return new GitDetectionResult
            {
                Success = false,
                Message = "Git command failed or repository not found."
            };
        }

        public static (List<string> Repos, List<string> Skipped) SafeEnumerateGitRepos(string root)
        {
            var repos = new List<string>();
            var skipped = new List<string>();

            void Recurse(string current)
            {
                try
                {
                    foreach (var dir in Directory.EnumerateDirectories(current))
                    {
                        try
                        {
                            if (Directory.Exists(Path.Combine(dir, ".git")))
                            {
                                repos.Add(dir);
                            }
                            Recurse(dir);
                        }
                        catch (Exception ex)
                        {
                            skipped.Add($"{dir} — {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    skipped.Add($"{current} — {ex.Message}");
                }
            }

            Recurse(root);
            return (repos, skipped);
        }

    }
}
