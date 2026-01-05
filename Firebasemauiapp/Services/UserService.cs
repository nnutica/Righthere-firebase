using System.Threading.Tasks;
using Firebase.Auth;
using Microsoft.Maui.Storage;

namespace Firebasemauiapp.Services;

/// <summary>
/// Centralized service to manage current user information
/// </summary>
public class UserService
{
    private static UserService? _instance;
    public static UserService Instance => _instance ??= new UserService();

    private readonly FirebaseAuthClient? _authClient;

    // ? Public properties ???????????????? user
    public string Username { get; private set; } = "Guest";
    public string Email { get; private set; } = "";
    public string Uid { get; private set; } = "";
    public bool IsGoogleUser { get; private set; } = false;
    public bool IsLoaded { get; private set; } = false;

    private UserService()
    {
        // ? ?????? GetService ???? Get
        try
        {
            _authClient = ServiceHelper.Get<FirebaseAuthClient>();
        }
        catch
        {
            _authClient = null;
            Console.WriteLine("[UserService] FirebaseAuthClient not available in ServiceHelper");
        }
    }

    /// <summary>
    /// Load user information from Firebase or Google Auth
    /// Call this after successful login
    /// </summary>
    public async Task LoadUserAsync()
    {
        try
        {
            // 1?? Try Firebase user first (Email/Password login)
            if (_authClient?.User != null)
            {
                var user = _authClient.User;
                Uid = user.Uid ?? "";
                Email = user.Info?.Email ?? "";
                
                var displayName = user.Info?.DisplayName;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    Username = !string.IsNullOrWhiteSpace(Email) && Email.Contains('@')
                        ? Email.Split('@')[0]
                        : "Friend";
                }
                else
                {
                    Username = displayName;
                }
                
                IsGoogleUser = false;
                IsLoaded = true;
                
                Console.WriteLine($"[UserService] Loaded Firebase user: {Username} ({Email})");
                return;
            }

            // 2?? Fallback: Try Google user
            var googleUser = await GoogleAuthService.Instance.GetGoogleUserAsync();
            if (googleUser != null)
            {
                Uid = googleUser.Uid;
                Email = googleUser.Email;
                
                if (!string.IsNullOrWhiteSpace(googleUser.DisplayName))
                {
                    Username = googleUser.DisplayName;
                }
                else if (!string.IsNullOrWhiteSpace(googleUser.Email))
                {
                    Username = googleUser.Email.Contains('@')
                        ? googleUser.Email.Split('@')[0]
                        : "Friend";
                }
                else
                {
                    Username = "Friend";
                }
                
                IsGoogleUser = true;
                IsLoaded = true;
                
                Console.WriteLine($"[UserService] Loaded Google user: {Username} ({Email})");
                return;
            }

            // 3?? Fallback: Try from cached UID in Preferences
            var cachedUid = Preferences.Get("AUTH_UID", null);
            if (!string.IsNullOrWhiteSpace(cachedUid))
            {
                Uid = cachedUid;
                IsGoogleUser = Preferences.Get("IS_GOOGLE_USER", false);
                
                // Try to get email/username from SecureStorage if Google user
                if (IsGoogleUser)
                {
                    Email = await SecureStorage.GetAsync("GOOGLE_EMAIL") ?? "";
                    Username = await SecureStorage.GetAsync("GOOGLE_DISPLAY_NAME") ?? "Friend";
                }
                
                IsLoaded = true;
                Console.WriteLine($"[UserService] Loaded from cache: {Username} (UID: {Uid})");
                return;
            }

            // 4?? Default
            Username = "Guest";
            Email = "";
            Uid = "";
            IsGoogleUser = false;
            IsLoaded = true;
            
            Console.WriteLine("[UserService] No user found, defaulting to Guest");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserService] Error loading user: {ex.Message}");
            Username = "Guest";
            Email = "";
            Uid = "";
            IsGoogleUser = false;
            IsLoaded = true;
        }
    }

    /// <summary>
    /// Clear user data (call on logout)
    /// </summary>
    public void Clear()
    {
        Username = "Guest";
        Email = "";
        Uid = "";
        IsGoogleUser = false;
        IsLoaded = false;
        
        Console.WriteLine("[UserService] User data cleared");
    }

    /// <summary>
    /// Refresh user data
    /// </summary>
    public async Task RefreshAsync()
    {
        IsLoaded = false;
        await LoadUserAsync();
    }

    /// <summary>
    /// delete account and all data
    /// </summary>
    public async Task DeleteAccountAsync()
    {
        try
        {
            Console.WriteLine($"[UserService] Starting account deletion for {Uid}...");
            
            // 1. Delete Firestore Data FIRST (while we still have auth permission if needed)
            // Note: If you need ID token verification for backend, do it before deleting Auth user.
            // Using Admin SDK in FirestoreService (if configured) or Client SDK.
            // Assuming FirestoreService uses Admin SDK or rules allow user to delete their own data.
            try 
            {
                var firestoreService = ServiceHelper.Get<FirestoreService>();
                if (firestoreService != null && !string.IsNullOrEmpty(Uid))
                {
                    await firestoreService.DeleteUserDataAsync(Uid);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserService] Warning: Failed to delete Firestore data: {ex.Message}");
                // Continue to delete Auth user anyway? Or stop? 
                // Usually better to ensure Auth is gone so they can't login again.
            }

            // 2. Delete Google Auth connection (if applicable)
            if (IsGoogleUser)
            {
                 try
                {
                    await GoogleAuthService.Instance.SignOutAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UserService] Google disconnect error: {ex.Message}");
                }
            }

            // 3. Delete Firebase Auth User
            if (_authClient?.User != null)
            {
                await _authClient.User.DeleteAsync();
                Console.WriteLine("[UserService] Firebase Auth user deleted");
            }
            else
            {
                Console.WriteLine("[UserService] No active Firebase user to delete");
            }

            // 4. Clear Local Data
            Clear();
            SecureStorage.Default.RemoveAll();
            Preferences.Default.Clear();

            Console.WriteLine("[UserService] Account deletion complete.");

        }
        catch (Exception ex)
        {
             Console.WriteLine($"[UserService] Fatal error during account deletion: {ex.Message}");
             throw; // Re-throw to let ViewModel handle UI feedback
        }
    }
}
