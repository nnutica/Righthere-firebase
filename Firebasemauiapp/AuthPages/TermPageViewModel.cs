using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;
using System.Diagnostics;

namespace Firebasemauiapp.Pages;

public partial class TermPageViewModel : ObservableObject
{
    private readonly FirestoreService _firestoreService;

    public TermPageViewModel(FirestoreService firestoreService)
    {
        _firestoreService = firestoreService;
    }

    [RelayCommand]
    private async Task Close()
    {
        try
        {
            // Check if user came from Google signup flow
            bool isFromGoogleSignup = Preferences.Get("GOOGLE_SIGNUP_FLOW", false);
            
            if (isFromGoogleSignup)
            {
                // Update user consent flags in Firestore
                var uid = Preferences.Get("AUTH_UID", string.Empty);
                if (!string.IsNullOrEmpty(uid))
                {
                    var db = await _firestoreService.GetDatabaseAsync();
                    var userDoc = db.Collection("users").Document(uid);
                    var updates = new Dictionary<string, object>
                    {
                        { "agree_TOS", true },
                        { "agree_AIAnalysis", true }
                    };
                    await userDoc.UpdateAsync(updates);
                    Debug.WriteLine("[TermPageViewModel] Updated consent flags for user: " + uid);
                }
                
                // Clear the flag
                Preferences.Remove("GOOGLE_SIGNUP_FLOW");
                
                // Navigate to starter page
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("//starter", false);
            }
            else
            {
                // User came from Terms/Privacy link in SignUp, go back to SignUp
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("//signup", false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TermPageViewModel] Error in Close command: {ex.Message}");
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Error", $"Navigation failed: {ex.Message}", "OK");
        }
    }
}
