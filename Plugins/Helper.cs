using System.Diagnostics;

namespace SemanticKernelPlayground.Plugins
{
    public static class Helper
    {
        public static string? GetVersionFilePath()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    var versionPath = Path.Combine(dir.FullName, "VERSION");

                    if (!File.Exists(versionPath))
                    {
                        File.WriteAllText(versionPath, "0.0.0");
                    }

                    return versionPath;
                }

                dir = dir.Parent;
            }

            return null;
        }

        public static string? TryAutoDetectRepo()
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
                    return process.StandardOutput.ReadToEnd().Trim();
                }
            }
            catch
            {
                // Silent fail
            }

            return null;
        }
    }
}
