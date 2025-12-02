using Firebase.Auth;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;
using Microsoft.Maui.Controls.Shapes;

namespace Firebasemauiapp.Mainpages;

public partial class PotSelectionPopup : Border
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;
    private TaskCompletionSource<string?>? _tcs;

    public PotSelectionPopup()
    {
        InitializeComponent();

        _authClient = ServiceHelper.Get<FirebaseAuthClient>();
        _firestoreService = ServiceHelper.Get<FirestoreService>();
    }

    public async Task<string?> ShowAsync(string currentPotImage)
    {
        _tcs = new TaskCompletionSource<string?>();

        // Load user's pot inventory
        await LoadUserPotsAsync(currentPotImage);

        // Show popup
        IsVisible = true;
        PopupContainer.Scale = 0.8;
        PopupContainer.Opacity = 0;

        await Task.WhenAll(
            PopupContainer.ScaleToAsync(1.0, 250, Easing.CubicOut),
            PopupContainer.FadeToAsync(1.0, 250)
        );

        return await _tcs.Task;
    }

    private async Task LoadUserPotsAsync(string currentPotImage)
    {
        try
        {
            var uid = _authClient.User?.Uid;
            if (string.IsNullOrWhiteSpace(uid)) return;

            var db = await _firestoreService.GetDatabaseAsync();
            var userDocRef = db.Collection("users").Document(uid);
            var snapshot = await userDocRef.GetSnapshotAsync();

            if (!snapshot.Exists) return;

            var userData = snapshot.ToDictionary();

            // Always include default pot
            var availablePots = new List<string> { "pot.png" };

            // Add purchased pots from inventory
            if (userData.ContainsKey("inventory"))
            {
                var inventory = userData["inventory"] as List<object>;
                if (inventory != null)
                {
                    // Map of item IDs to their pot images
                    var potItems = new Dictionary<string, string>
                    {
                        { "starry_nest", "starrynest.png" },
                        { "bloombox", "bloombox.png" },
                        { "bath_blossom", "bathblossom.png" }
                    };

                    foreach (var itemId in inventory)
                    {
                        if (itemId != null && potItems.ContainsKey(itemId.ToString()!))
                        {
                            availablePots.Add(potItems[itemId.ToString()!]);
                        }
                    }
                }
            }

            // Build UI for pots
            PotsFlexLayout.Children.Clear();

            foreach (var potImage in availablePots)
            {
                var isSelected = potImage == currentPotImage;

                var border = new Border
                {
                    BackgroundColor = isSelected ? Color.FromArgb("#E8F5E9") : Colors.Transparent,
                    StrokeThickness = isSelected ? 3 : 1,
                    Stroke = isSelected ? Color.FromArgb("#50A65D") : Color.FromArgb("#E0E0E0"),
                    WidthRequest = 130,
                    HeightRequest = 130,
                    Margin = new Thickness(6),
                    StrokeShape = new RoundRectangle { CornerRadius = 12 }
                };

                var image = new Image
                {
                    Source = potImage,
                    WidthRequest = 110,
                    HeightRequest = 110,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                border.Content = image;

                // Add tap handler
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) =>
                {
                    await OnPotSelected(potImage);
                };
                border.GestureRecognizers.Add(tapGesture);

                PotsFlexLayout.Children.Add(border);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading pots: {ex.Message}");
        }
    }

    private async Task OnPotSelected(string potImage)
    {
        try
        {
            var uid = _authClient.User?.Uid;
            if (string.IsNullOrWhiteSpace(uid)) return;

            var db = await _firestoreService.GetDatabaseAsync();
            var userDocRef = db.Collection("users").Document(uid);

            // Update currentPot in Firestore
            await userDocRef.UpdateAsync(new Dictionary<string, object>
            {
                { "currentPot", potImage }
            });

            // Close popup and return selected pot
            await ClosePopup();
            _tcs?.SetResult(potImage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating pot: {ex.Message}");
            await ClosePopup();
            _tcs?.SetResult(null);
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await ClosePopup();
        _tcs?.SetResult(null);
    }

    private async void OnBackgroundTapped(object? sender, EventArgs e)
    {
        await ClosePopup();
        _tcs?.SetResult(null);
    }

    private async Task ClosePopup()
    {
        await Task.WhenAll(
            PopupContainer.ScaleToAsync(0.8, 200, Easing.CubicIn),
            PopupContainer.FadeToAsync(0, 200)
        );

        IsVisible = false;
    }
}
