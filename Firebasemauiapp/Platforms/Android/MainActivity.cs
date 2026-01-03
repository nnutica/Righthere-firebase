using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Gms.Auth.Api.SignIn;
using Firebasemauiapp.Services;
using Java.Security;
using Android.Util;

namespace Firebasemauiapp;

[Activity(Theme = "@style/MainTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        // Set theme before calling base OnCreate
        SetTheme(Resource.Style.MainTheme);
        base.OnCreate(savedInstanceState);
        
        // Log SHA-1 fingerprint for debugging Google Sign-In
        try
        {
            var packageInfo = PackageManager?.GetPackageInfo(PackageName!, PackageInfoFlags.Signatures);
            if (packageInfo?.Signatures != null)
            {
                foreach (var signature in packageInfo.Signatures)
                {
                    var md = MessageDigest.GetInstance("SHA-1");
                    md?.Update(signature.ToByteArray());
                    var sha1 = Convert.ToBase64String(md?.Digest() ?? Array.Empty<byte>());
                    
                    // Also get hex format
                    var hexBytes = md?.Digest();
                    var sha1Hex = hexBytes != null ? BitConverter.ToString(hexBytes).Replace("-", ":") : "";
                    
                    Console.WriteLine($"[MainActivity.OnCreate] ==========================================");
                    Console.WriteLine($"[MainActivity.OnCreate] Package Name: {PackageName}");
                    Console.WriteLine($"[MainActivity.OnCreate] SHA-1 (Base64): {sha1}");
                    Console.WriteLine($"[MainActivity.OnCreate] SHA-1 (Hex): {sha1Hex}");
                    Console.WriteLine($"[MainActivity.OnCreate] ==========================================");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MainActivity.OnCreate] Error getting SHA-1: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle Google Sign-In result from OnActivityResult
    /// </summary>
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        Console.WriteLine($"[MainActivity.OnActivityResult] ===== START =====");
        Console.WriteLine($"[MainActivity.OnActivityResult] requestCode={requestCode}, resultCode={resultCode}");
        Console.WriteLine($"[MainActivity.OnActivityResult] data is null: {data == null}");
        if (data != null)
        {
            Console.WriteLine($"[MainActivity.OnActivityResult] data Action: {data.Action}");
            Console.WriteLine($"[MainActivity.OnActivityResult] data Extras: {(data.Extras != null ? data.Extras.KeySet().Count : 0)} items");
            if (data.Extras != null)
            {
                foreach (var key in data.Extras.KeySet())
                {
                    Console.WriteLine($"[MainActivity.OnActivityResult]   - {key}");
                }
            }
        }

        // Request code for Google Sign-In
        if (requestCode == 9001)
        {
            Console.WriteLine("[MainActivity.OnActivityResult] Processing Google Sign-In result (requestCode 9001)");
            
            if (data == null)
            {
                Console.WriteLine("[MainActivity.OnActivityResult] ❌ Data is null - user cancelled or error");
                GoogleSignInResultHandler.Instance.SetAccountResult(null);
                Console.WriteLine($"[MainActivity.OnActivityResult] ===== END =====");
                return;
            }

            // Check googleSignInStatus in extras
            if (data.Extras != null && data.Extras.ContainsKey("googleSignInStatus"))
            {
                var statusObj = data.Extras.Get("googleSignInStatus");
                Console.WriteLine($"[MainActivity.OnActivityResult] googleSignInStatus type: {statusObj?.GetType().Name}");
                Console.WriteLine($"[MainActivity.OnActivityResult] googleSignInStatus: {statusObj}");
                
                // Try to get status code if it's a Status object
                try
                {
                    var statusType = statusObj?.GetType();
                    var statusCodeProp = statusType?.GetProperty("StatusCode");
                    if (statusCodeProp != null)
                    {
                        var statusCode = statusCodeProp.GetValue(statusObj);
                        Console.WriteLine($"[MainActivity.OnActivityResult] Status Code: {statusCode}");
                    }
                    
                    var statusMessageProp = statusType?.GetProperty("StatusMessage");
                    if (statusMessageProp != null)
                    {
                        var statusMessage = statusMessageProp.GetValue(statusObj);
                        Console.WriteLine($"[MainActivity.OnActivityResult] Status Message: {statusMessage}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MainActivity.OnActivityResult] Could not read status details: {ex.Message}");
                }
            }

            // Try to get account regardless of result code
            // Sometimes Google Sign-In returns RESULT_CANCELED but still has account data
            try
            {
                Console.WriteLine("[MainActivity.OnActivityResult] Attempting to extract account from intent...");
                
                // Extract Google Sign-In account from the intent result
                var task = GoogleSignIn.GetSignedInAccountFromIntent(data);
                
                Console.WriteLine($"[MainActivity.OnActivityResult] Task successful: {task.IsSuccessful}, IsComplete: {task.IsComplete}");
                
                if (task.IsSuccessful)
                {
                    var account = task.Result as GoogleSignInAccount;
                    Console.WriteLine($"[MainActivity.OnActivityResult] ✓ Got account: {account?.Email}");
                    Console.WriteLine($"[MainActivity.OnActivityResult]   Display Name: {account?.DisplayName}");
                    Console.WriteLine($"[MainActivity.OnActivityResult]   IdToken exists: {(account?.IdToken != null ? "yes" : "no")}");
                    if (account?.IdToken != null)
                    {
                        Console.WriteLine($"[MainActivity.OnActivityResult]   IdToken length: {account.IdToken.Length}");
                    }
                    GoogleSignInResultHandler.Instance.SetAccountResult(account);
                }
                else
                {
                    var exception = task.Exception;
                    Console.WriteLine($"[MainActivity.OnActivityResult] ❌ Google Sign-In task not successful");
                    Console.WriteLine($"[MainActivity.OnActivityResult]   Exception: {exception?.Message}");
                    if (exception?.InnerException != null)
                    {
                        Console.WriteLine($"[MainActivity.OnActivityResult]   Inner Exception: {exception.InnerException.Message}");
                    }
                    
                    // Check if this is an API error
                    if (exception is Android.Gms.Common.Apis.ApiException apiEx)
                    {
                        Console.WriteLine($"[MainActivity.OnActivityResult]   API Exception StatusCode: {apiEx.StatusCode}");
                    }
                    
                    GoogleSignInResultHandler.Instance.SetAccountResult(null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainActivity.OnActivityResult] ❌ Exception caught: {ex.GetType().Name}");
                Console.WriteLine($"[MainActivity.OnActivityResult]   Message: {ex.Message}");
                Console.WriteLine($"[MainActivity.OnActivityResult]   StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[MainActivity.OnActivityResult]   Inner Exception: {ex.InnerException.Message}");
                }
                GoogleSignInResultHandler.Instance.SetAccountResult(null);
            }
            finally
            {
                Console.WriteLine($"[MainActivity.OnActivityResult] ===== END =====");
            }
        }
        else
        {
            Console.WriteLine($"[MainActivity.OnActivityResult] ⊘ Ignoring unknown requestCode: {requestCode}");
        }
    }
}
