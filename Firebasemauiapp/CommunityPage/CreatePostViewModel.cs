using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Data;
using Firebasemauiapp.Model;

namespace Firebasemauiapp.CommunityPage;

public partial class CreatePostViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly PostDatabase _postDb;

    public CreatePostViewModel(FirebaseAuthClient authClient, PostDatabase postDb)
    {
        _authClient = authClient;
        _postDb = postDb;

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

            var user = _authClient.User;
            var userId = user?.Uid ?? "Anonymous";

            var newPost = new PostData
            {
                Content = PostContent,
                Author = userId,
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
