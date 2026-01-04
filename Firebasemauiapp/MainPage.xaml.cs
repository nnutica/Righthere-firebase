using System.Security.Cryptography;

namespace Firebasemauiapp;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();

        // เพิ่มส่วนเช็ค SHA-1 ตรงนี้ครับ
        CheckSha1Fingerprint();
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

    private void OnCounterClicked(object? sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }
}