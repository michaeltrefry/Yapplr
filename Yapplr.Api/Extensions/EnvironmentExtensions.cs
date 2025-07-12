namespace Yapplr.Api.Extensions;

public static class EnvironmentExtensions
{
    public static void ConfigureEnvironmentFromGitBranch()
    {
        // Auto-detect environment based on Git branch ONLY in development scenarios
        // This prevents auto-detection from running in staging/production deployments
        if (IsLocalDevelopment())
        {
            try
            {
                var gitBranch = GetCurrentGitBranch();
                var environment = (gitBranch == "main" || gitBranch == "master") ? "Test" : "Development";
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
                Console.WriteLine($"🔄 Auto-detected Git branch '{gitBranch}' -> Using {environment} environment");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Could not auto-detect Git branch: {ex.Message}. Using default environment.");
            }
        }
    }

    private static bool IsLocalDevelopment()
    {
        // Check if we're running in a local development environment
        // This prevents auto-detection from running in staging/production
        try
        {
            // Check for common development indicators
            var isDevelopment =
                // Running from source directory (has .git folder)
                Directory.Exists(".git") ||
                Directory.Exists("../.git") ||
                // Running with dotnet run (not published)
                Environment.CommandLine.Contains("dotnet run") ||
                // Local development URLs
                Environment.GetEnvironmentVariable("ASPNETCORE_URLS")?.Contains("localhost") == true ||
                // Development machine indicators
                Environment.MachineName.ToLower().Contains("dev") ||
                Environment.UserName.ToLower().Contains("dev") ||
                // Not running in container
                !File.Exists("/.dockerenv");

            return isDevelopment;
        }
        catch
        {
            // If we can't determine, err on the side of caution
            return false;
        }
    }

    private static string GetCurrentGitBranch()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "branch --show-current",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                return output;
            }

            throw new Exception("Git command failed or returned empty result");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to execute git command: {ex.Message}");
        }
    }
}
