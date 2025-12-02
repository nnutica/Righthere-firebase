using CommunityToolkit.Mvvm.ComponentModel;

namespace Firebasemauiapp.Model;

public partial class StoreItem : ObservableObject
{
    [ObservableProperty]
    private string id = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string imageSource = string.Empty;

    [ObservableProperty]
    private int price;

    [ObservableProperty]
    private bool isSoldOut;

    public StoreItem(string id, string name, string imageSource, int price)
    {
        Id = id;
        Name = name;
        ImageSource = imageSource;
        Price = price;
        IsSoldOut = false;
    }
}
