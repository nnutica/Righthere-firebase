using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;
using Microsoft.Maui.Storage;

namespace Firebasemauiapp.CommunityPage;

public partial class CreatePostViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly PostDatabase _postDb;
    private readonly FirestoreService _firestoreService;

    public CreatePostViewModel(FirebaseAuthClient authClient, PostDatabase postDb, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _postDb = postDb;
        _firestoreService = firestoreService;

        // Load available colors
        AvailableColors = new ObservableCollection<PostItColorOption>(PostItColorOption.GetAllColors());

        // Select first color by default
        if (AvailableColors.Count > 0)
        {
            SelectedColor = AvailableColors[0];
        }
    }

    [ObservableProperty]
    private ObservableCollection<PostItColorOption> availableColors;

    [ObservableProperty]
    private PostItColorOption? selectedColor;

    [ObservableProperty]
    private string postContent = string.Empty;

    [ObservableProperty]
    private bool isPosting = false;

    [RelayCommand]
    private void SelectColor(PostItColorOption color)
    {
        SelectedColor = color;
    }

    [RelayCommand]
    private async Task SharePostAsync()
    {
        if (string.IsNullOrWhiteSpace(PostContent))
        {
            await Shell.Current.DisplayAlert("Error", "Please write something before sharing.", "OK");
            return;
        }

        if (SelectedColor == null)
        {
            await Shell.Current.DisplayAlert("Error", "Please select a color.", "OK");
            return;
        }

        try
        {
            IsPosting = true;

            // ดึง UID จาก Preferences
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            var userName = "Anonymous";

            // ถ้ามี UID ให้ดึง username จาก Firestore
            if (!string.IsNullOrEmpty(uid))
            {
                var username = await _firestoreService.GetUsernameAsync(uid);
                userName = string.IsNullOrEmpty(username) ? uid : username;
            }

            var newPost = new PostData
            {
                Content = PostContent,
                Author = userName,
                PostItColor = SelectedColor.PostItImage,
                TextColor = SelectedColor.TextColor,
                Likes = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _postDb.CreatePostAsync(newPost);

            // Clear form
            PostContent = string.Empty;

            // Navigate back
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to create post: {ex.Message}", "OK");
        }
        finally
        {
            IsPosting = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}
