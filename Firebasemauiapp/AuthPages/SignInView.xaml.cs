namespace Firebasemauiapp.Pages;

using System.Security.Cryptography;
public partial class SignInView : ContentPage
{
	public SignInView(SignInViewModel viewModel)
	{
		InitializeComponent();
        CheckSha1Fingerprint();

        BindingContext = viewModel;
	}

    private void CheckSha1Fingerprint()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            // ดึงข้อมูล Package และ Signature
            var info = context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Signatures);

            foreach (var signature in info.Signatures)
            {
                using (var sha1 = SHA1.Create())
                {
                    var hash = sha1.ComputeHash(signature.ToByteArray());
                    var hex = BitConverter.ToString(hash).Replace("-", ":");

                    // *** บรรทัดนี้จะแสดง SHA-1 ใน Output ***
                    Console.WriteLine($"\n\n==========================================");
                    Console.WriteLine($"[MY REAL SHA-1] : {hex}");
                    Console.WriteLine($"==========================================\n\n");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking SHA-1: {ex.Message}");
        }
#endif
    }


	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Clear user authentication when entering Sign In page
		Preferences.Remove("AUTH_UID");
		Preferences.Remove("USER_DISPLAY_NAME");

		System.Diagnostics.Debug.WriteLine("[SignInView] Cleared user authentication data");
	}

	private async void OnGoogleSignInClicked(object sender, EventArgs e)
	{
		await DisplayAlert("Google Sign-In", "ยังไม่พร้อมใช้งาน", "OK");
	}
}