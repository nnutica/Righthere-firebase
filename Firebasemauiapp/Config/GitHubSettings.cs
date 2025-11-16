using Microsoft.Maui.Storage;

namespace Firebasemauiapp.Config;

// NOTE: For development/demo only. Do NOT commit real tokens to source control.
// Fill the fields below, or leave empty and set Preferences at runtime from a settings screen.
public static class GitHubSettings
{
    public static string Owner { get; set; } = "nnutica";            // e.g. "your-github-username"
    public static string Repo { get; set; } = "Righthere_storage";             // e.g. "your-repo-name"
    public static string Token { get; set; } = " ";            // e.g. "ghp_xxx..." (repo scope)
    public static string Branch { get; set; } = "main";       // optional
    public static string PathPrefix { get; set; } = "uploads/diary"; // optional subfolder

    // Apply values to Preferences if not already set
    public static void ApplyToPreferences()
    {
        TrySetPref("GitHubOwner", Owner);
        TrySetPref("GitHubRepo", Repo);
        TrySetPref("GitHubToken", Token);
        TrySetPref("GitHubBranch", Branch);
        TrySetPref("GitHubPathPrefix", PathPrefix);
    }

    private static void TrySetPref(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(Preferences.Default.Get(key, string.Empty)) && !string.IsNullOrWhiteSpace(value))
        {
            Preferences.Default.Set(key, value);
        }
    }
}
