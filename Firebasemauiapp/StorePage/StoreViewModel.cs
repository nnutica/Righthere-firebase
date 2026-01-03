using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebasemauiapp.Services;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.StorePage;

public partial class StoreViewModel : ObservableObject, IDisposable
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirestoreService _firestoreService;
    private bool _disposed;

    [ObservableProperty]
    private int _coin;

    [ObservableProperty]
    private ObservableCollection<StoreItem> _storeItems = new();

    [ObservableProperty]
    private string _plantImage = "plant.png";

    [ObservableProperty]
    private string _currentPot = "pot.png";

    [ObservableProperty]
    private bool _isStoreTabSelected = true;

    [ObservableProperty]
    private ObservableCollection<StoreItem> _myItems = new();

    [ObservableProperty]
    private ObservableCollection<StoreItem> _availableStoreItems = new();

    public StoreViewModel(FirebaseAuthClient authClient, FirestoreService firestoreService)
    {
        _authClient = authClient;
        _firestoreService = firestoreService;

        _authClient.AuthStateChanged += OnAuthStateChanged;

        LoadStoreItems();
        _ = RefreshDataAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _authClient.AuthStateChanged -= OnAuthStateChanged;
        _disposed = true;
    }

    [RelayCommand]
    private async Task GoBack()
    {
        try
        {
            if (Shell.Current != null)
                await Shell.Current.GoToAsync("//main/starter");
        }
        catch (Exception ex)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert("Navigation error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private void SelectStoreTab()
    {
        IsStoreTabSelected = true;
    }

    [RelayCommand]
    private void SelectMyItemsTab()
    {
        IsStoreTabSelected = false;
    }

    [RelayCommand]
    private async Task UseItem(StoreItem item)
    {
        if (item == null) return;

        try
        {
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            if (string.IsNullOrEmpty(uid))
            {
                await Shell.Current.DisplayAlert("Error", "Please log in first.", "OK");
                return;
            }

            var db = await _firestoreService.GetDatabaseAsync();
            var userDoc = db.Collection("users").Document(uid);

            // Update currentPot in Firestore
            await userDoc.UpdateAsync(new Dictionary<string, object>
            {
                { "currentPot", item.Image }
            });

            CurrentPot = item.Image;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to use item: {ex.Message}", "OK");
        }
    }

    private void OnAuthStateChanged(object? sender, UserEventArgs e)
    {
        if (_disposed) return;

        if (MainThread.IsMainThread)
            _ = RefreshDataAsync();
        else
            MainThread.BeginInvokeOnMainThread(() => _ = RefreshDataAsync());
    }

    private async Task RefreshDataAsync()
    {
        if (_disposed) return;

        await RefreshCoinAsync();
        await LoadInventoryAsync();
    }
    private void LoadStoreItems()
    {
        StoreItems = new ObservableCollection<StoreItem>
        {
            new StoreItem
            {
                Name = "Starry Nest",
                Price = 550,
                Image = "starrynest.png",
                ItemType = "decoration",
                ItemId = "starry_nest"
            },
            new StoreItem
            {
                Name = "Box",
                Price = 150,
                Image = "box.png",
                ItemType = "decoration",
                ItemId = "box"
            },
            new StoreItem
            {
                Name = "Bath Blossom",
                Price = 350,
                Image = "bathblossom.png",
                ItemType = "decoration",
                ItemId = "bath_blossom"
            }
        };
    }

    private async Task RefreshCoinAsync()
    {
        try
        {
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            if (string.IsNullOrEmpty(uid))
            {
                Coin = 0;
                return;
            }

            Coin = await _firestoreService.GetCoinAsync(uid);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RefreshCoinAsync Error: {ex.Message}");
            Coin = 0;
        }
    }

    private async Task LoadInventoryAsync()
    {
        try
        {
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            if (string.IsNullOrEmpty(uid))
            {
                // Clear purchased status if not logged in
                foreach (var item in StoreItems)
                {
                    item.IsPurchased = false;
                }
                return;
            }

            var db = await _firestoreService.GetDatabaseAsync();
            var userDoc = await db.Collection("users").Document(uid).GetSnapshotAsync();

            HashSet<string> purchasedItemIds = new HashSet<string>();

            if (userDoc.Exists)
            {
                // Try to read inventory as array field first
                if (userDoc.TryGetValue("inventory", out object inventoryObj))
                {
                    if (inventoryObj is List<object> inventoryList)
                    {
                        foreach (var item in inventoryList)
                        {
                            if (item != null)
                            {
                                purchasedItemIds.Add(item.ToString()!);
                            }
                        }
                        Console.WriteLine($"✅ Loaded inventory (array): {string.Join(", ", purchasedItemIds)}");
                    }
                }
                // Fallback: Try subcollection if array field doesn't exist
                else
                {
                    var inventoryRef = db.Collection("users").Document(uid).Collection("inventory");
                    var inventorySnap = await inventoryRef.GetSnapshotAsync();

                    purchasedItemIds = inventorySnap.Documents
                        .Select(doc => doc.Id)
                        .ToHashSet();

                    Console.WriteLine($"✅ Loaded inventory (subcollection): {purchasedItemIds.Count} items");
                }
            }

            // Update purchased status for each store item
            foreach (var item in StoreItems)
            {
                item.IsPurchased = purchasedItemIds.Contains(item.ItemId);
                Console.WriteLine($"Item: {item.ItemId} - Purchased: {item.IsPurchased}");
            }

            // Update MyItems collection with purchased items
            MyItems.Clear();

            // Always add default pot first
            MyItems.Add(new StoreItem
            {
                Name = "Default Pot",
                Image = "pot.png",
                ItemType = "pot",
                ItemId = "pot",
                IsPurchased = true
            });

            foreach (var item in StoreItems.Where(i => i.IsPurchased))
            {
                MyItems.Add(item);
            }

            // Update AvailableStoreItems to show only unpurchased items
            AvailableStoreItems.Clear();
            foreach (var item in StoreItems.Where(i => !i.IsPurchased))
            {
                AvailableStoreItems.Add(item);
            }

            // Load current pot and plant
            if (userDoc.TryGetValue("currentPot", out string currentPot))
            {
                CurrentPot = currentPot;
            }
            if (userDoc.TryGetValue("currentPlant", out string currentPlant))
            {
                PlantImage = currentPlant;
            }

            Console.WriteLine($"✅ Total purchased items: {purchasedItemIds.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadInventoryAsync Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task PurchaseItem(StoreItem item)
    {
        if (item == null) return;

        try
        {
            var uid = Preferences.Get("AUTH_UID", string.Empty);
            if (string.IsNullOrEmpty(uid))
            {
                await Shell.Current.DisplayAlert("Error", "Please log in first.", "OK");
                return;
            }

            // Check if already purchased
            if (item.IsPurchased)
            {
                await Shell.Current.DisplayAlert("Already Purchased",
                    $"You already own {item.Name}!", "OK");
                return;
            }

            // Check if user has enough coins
            if (Coin < item.Price)
            {
                await Shell.Current.DisplayAlert("Insufficient Coins",
                    $"You need {item.Price} coins but only have {Coin} coins.", "OK");
                return;
            }

            // Confirm purchase
            bool confirm = await Shell.Current.DisplayAlert("Confirm Purchase",
                $"Do you want to buy {item.Name} for {item.Price} coins?", "Yes", "No");

            if (!confirm) return;

            var db = await _firestoreService.GetDatabaseAsync();
            var userRef = db.Collection("users").Document(uid);

            // Transaction to deduct coins and add item to inventory array
            await db.RunTransactionAsync(async tx =>
            {
                var userSnap = await tx.GetSnapshotAsync(userRef);
                if (!userSnap.Exists)
                {
                    throw new Exception("User document not found");
                }

                int currentCoin = userSnap.TryGetValue("coin", out int c) ? c : 0;

                if (currentCoin < item.Price)
                {
                    throw new Exception("Insufficient coins");
                }

                // Get current inventory array
                List<string> currentInventory = new List<string>();
                if (userSnap.TryGetValue("inventory", out object inventoryObj))
                {
                    if (inventoryObj is List<object> invList)
                    {
                        currentInventory = invList.Select(i => i.ToString()!).ToList();
                    }
                }

                // Check if already purchased (double-check in transaction)
                if (currentInventory.Contains(item.ItemId))
                {
                    throw new Exception("Item already purchased");
                }

                // Add item to inventory
                currentInventory.Add(item.ItemId);

                // Deduct coins and update inventory
                int newCoin = currentCoin - item.Price;
                tx.Update(userRef, new Dictionary<string, object>
                {
                    { "coin", newCoin },
                    { "inventory", currentInventory }
                });
            });

            await Shell.Current.DisplayAlert("Success", $"You purchased {item.Name}!", "OK");

            // Refresh coin balance and inventory
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PurchaseItem Error: {ex.Message}");
            await Shell.Current.DisplayAlert("Error", $"Purchase failed: {ex.Message}", "OK");
        }
    }
}

public partial class StoreItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string ItemId { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isPurchased;

    public string PurchaseStatusText => IsPurchased ? "Purchased" : $"{Price}";
    public Color StatusColor => IsPurchased ? Color.FromArgb("#999999") : Color.FromArgb("#FEAA3A");
}
