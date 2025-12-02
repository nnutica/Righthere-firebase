using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.StorePage;

public partial class StoreViewModel : ObservableObject
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;

    [ObservableProperty]
    private int userCoins;

    [ObservableProperty]
    private string currentPlantImage = "plant.png"; // Default plant

    [ObservableProperty]
    private string currentPotImage = "pot.png"; // Default pot

    [ObservableProperty]
    private ObservableCollection<StoreItem> storeItems;

    // Callbacks for custom popups
    public Func<string, string, string, string, Task<bool>>? ShowConfirmationPopup { get; set; }
    public Func<string, string, string, bool, Task<bool>>? ShowAlertPopup { get; set; }

    public StoreViewModel(FirebaseAuthClient authClient, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _firestoreService = firestoreService;

        StoreItems = new ObservableCollection<StoreItem>
        {
            new StoreItem("starry_nest", "Starry Nest", "starrynest.png", 1000),
            new StoreItem("bloombox", "BloomBox", "bloombox.png", 150),
            new StoreItem("bath_blossom", "Bath Blossom", "bathblossom.png", 550)
        };

        _ = LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        try
        {
            var uid = _authClient.User?.Uid;
            if (string.IsNullOrWhiteSpace(uid))
            {
                Console.WriteLine("StoreViewModel: No user ID found");
                return;
            }

            var db = await _firestoreService.GetDatabaseAsync();
            var userDocRef = db.Collection("users").Document(uid);
            var snapshot = await userDocRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var userData = snapshot.ToDictionary();

                // Load coins
                if (userData.ContainsKey("coin"))
                {
                    UserCoins = Convert.ToInt32(userData["coin"]);
                    Console.WriteLine($"StoreViewModel: Loaded coins: {UserCoins}");
                }

                // Load inventory and mark sold items
                if (userData.ContainsKey("inventory"))
                {
                    var inventory = userData["inventory"] as List<object>;
                    if (inventory != null)
                    {
                        Console.WriteLine($"StoreViewModel: Inventory count: {inventory.Count}");
                        foreach (var item in StoreItems)
                        {
                            if (inventory.Contains(item.Id))
                            {
                                item.IsSoldOut = true;
                                Console.WriteLine($"StoreViewModel: Item {item.Name} marked as sold out");
                            }
                        }
                    }
                }

                // Load current plant and pot (if saved)
                if (userData.ContainsKey("currentPlant"))
                {
                    var plantValue = userData["currentPlant"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(plantValue))
                    {
                        CurrentPlantImage = plantValue;
                        Console.WriteLine($"StoreViewModel: Loaded plant image: {CurrentPlantImage}");
                    }
                }

                if (userData.ContainsKey("currentPot"))
                {
                    var potValue = userData["currentPot"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(potValue))
                    {
                        CurrentPotImage = potValue;
                        Console.WriteLine($"StoreViewModel: Loaded pot image: {CurrentPotImage}");
                    }
                }

                Console.WriteLine($"StoreViewModel: Final - Plant: {CurrentPlantImage}, Pot: {CurrentPotImage}");
            }
            else
            {
                Console.WriteLine("StoreViewModel: User document does not exist");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading user data: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task PurchaseItem(StoreItem item)
    {
        try
        {
            // Check if item is sold out
            if (item.IsSoldOut)
            {
                if (ShowAlertPopup != null)
                {
                    await ShowAlertPopup("Sold Out", $"{item.Name} has already been purchased.", "OK", false);
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("ขายหมดแล้ว", "ไอเท็มนี้ถูกซื้อไปแล้ว", "ตกลง");
                }
                return;
            }

            // Check if user has enough coins
            if (UserCoins < item.Price)
            {
                if (ShowAlertPopup != null)
                {
                    await ShowAlertPopup(
                        "Insufficient Coins",
                        $"You need {item.Price} coins but only have {UserCoins} coins.\n\nComplete more diary entries to earn coins!",
                        "OK",
                        true);
                }
                else
                {
                    await Application.Current!.MainPage!.DisplayAlert("เหรียญไม่พอ", "คุณมีเหรียญไม่เพียงพอสำหรับการซื้อไอเท็มนี้", "ตกลง");
                }
                return;
            }

            // Show confirmation popup
            bool confirm = false;
            if (ShowConfirmationPopup != null)
            {
                confirm = await ShowConfirmationPopup(
                    "Confirm Purchase",
                    $"Do you want to buy {item.Name} for {item.Price} coins?\n\nYour balance: {UserCoins} coins\nAfter purchase: {UserCoins - item.Price} coins",
                    "Purchase",
                    "Cancel");
            }
            else
            {
                confirm = await Application.Current!.MainPage!.DisplayAlert(
                    "ยืนยันการซื้อ",
                    $"ต้องการซื้อ {item.Name} ในราคา {item.Price} เหรียญ?",
                    "ซื้อ",
                    "ยกเลิก");
            }

            if (!confirm) return;

            var uid = _authClient.User?.Uid;
            if (string.IsNullOrWhiteSpace(uid)) return;

            var db = await _firestoreService.GetDatabaseAsync();
            var userDocRef = db.Collection("users").Document(uid);

            // Update coins and inventory
            var newCoins = UserCoins - item.Price;

            var updates = new Dictionary<string, object>
            {
                { "coin", newCoins },
                { "inventory", FieldValue.ArrayUnion(item.Id) }
            };

            // Set as current pot when purchased
            updates["currentPot"] = item.ImageSource;

            await userDocRef.UpdateAsync(updates);

            // Update UI
            UserCoins = newCoins;
            item.IsSoldOut = true;
            CurrentPotImage = item.ImageSource; // Update display immediately

            // Show success message
            if (ShowAlertPopup != null)
            {
                await ShowAlertPopup("Success!", $"You've successfully purchased {item.Name}!\n\nNew balance: {UserCoins} coins", "OK", false);
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("สำเร็จ", $"ซื้อ {item.Name} เรียบร้อยแล้ว!", "ตกลง");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error purchasing item: {ex.Message}");
            if (ShowAlertPopup != null)
            {
                await ShowAlertPopup("Error", "An error occurred while processing your purchase. Please try again.", "OK", true);
            }
            else
            {
                await Application.Current!.MainPage!.DisplayAlert("ข้อผิดพลาด", "เกิดข้อผิดพลาดในการซื้อไอเท็ม", "ตกลง");
            }
        }
    }
}
