using Microsoft.Maui.Storage;
using System.Threading.Tasks;

namespace Firebasemauiapp.Services;

/// <summary>
/// Service to manage Google Sign-In session independently from FirebaseAuthClient
/// </summary>
public class GoogleAuthService
{
    private static GoogleAuthService? _instance;
    public static GoogleAuthService Instance => _instance ??= new GoogleAuthService();

    private GoogleAuthService() { }

    public async Task<GoogleUserInfo?> GetGoogleUserAsync()
    {
        try
        {
            // Check if user is a Google user
            var isGoogleUser = Preferences.Get("IS_GOOGLE_USER", false);
            if (!isGoogleUser)
                return null;

            var uid = await SecureStorage.GetAsync("GOOGLE_UID");
            if (string.IsNullOrWhiteSpace(uid))
                return null;

            var email = await SecureStorage.GetAsync("GOOGLE_EMAIL");
            var displayName = await SecureStorage.GetAsync("GOOGLE_DISPLAY_NAME");
            var idToken = await SecureStorage.GetAsync("GOOGLE_ID_TOKEN");
            var refreshToken = await SecureStorage.GetAsync("GOOGLE_REFRESH_TOKEN");

            return new GoogleUserInfo
            {
                Uid = uid,
                Email = email ?? "",
                DisplayName = displayName ?? "",
                IdToken = idToken ?? "",
                RefreshToken = refreshToken ?? ""
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            SecureStorage.Remove("GOOGLE_ID_TOKEN");
            SecureStorage.Remove("GOOGLE_REFRESH_TOKEN");
            SecureStorage.Remove("GOOGLE_UID");
            SecureStorage.Remove("GOOGLE_EMAIL");
            SecureStorage.Remove("GOOGLE_DISPLAY_NAME");
            Preferences.Remove("IS_GOOGLE_USER");
            Preferences.Remove("AUTH_UID");

            await Task.CompletedTask;
        }
        catch
        {
            // Ignore errors during sign out
        }
    }

    public async Task<bool> IsSignedInAsync()
    {
        var user = await GetGoogleUserAsync();
        return user != null && !string.IsNullOrWhiteSpace(user.Uid);
    }
}

public class GoogleUserInfo
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
