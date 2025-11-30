using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Firebasemauiapp;

[Activity(Theme = "@style/MainTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        // Set theme before calling base OnCreate
        SetTheme(Resource.Style.MainTheme);
        base.OnCreate(savedInstanceState);
    }
}
