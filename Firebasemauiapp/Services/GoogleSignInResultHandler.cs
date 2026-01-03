using Android.Gms.Auth.Api.SignIn;
using System;

namespace Firebasemauiapp.Services;

/// <summary>
/// Singleton to handle Google Sign-In results from OnActivityResult callback
/// </summary>
public class GoogleSignInResultHandler
{
    private static GoogleSignInResultHandler? _instance;
    private GoogleSignInAccount? _lastAccount;
    private TaskCompletionSource<GoogleSignInAccount?>? _accountTcs;

    public static GoogleSignInResultHandler Instance
    {
        get
        {
            _instance ??= new GoogleSignInResultHandler();
            return _instance;
        }
    }

    /// <summary>
    /// Set the Google account result from OnActivityResult
    /// </summary>
    public void SetAccountResult(GoogleSignInAccount? account)
    {
        Console.WriteLine($"[GoogleSignInResultHandler.SetAccountResult] Setting account: {(account != null ? account.Email : "null")}");
        _lastAccount = account;
        _accountTcs?.TrySetResult(account);
        _accountTcs = null;
    }

    /// <summary>
    /// Get the last signed-in account or wait for one asynchronously
    /// </summary>
    public async Task<GoogleSignInAccount?> GetAccountAsync(TimeSpan? timeout = null)
    {
        Console.WriteLine("[GoogleSignInResultHandler.GetAccountAsync] Checking for cached account...");
        
        // If we already have an account, return it
        if (_lastAccount != null && !string.IsNullOrEmpty(_lastAccount.IdToken))
        {
            Console.WriteLine($"[GoogleSignInResultHandler.GetAccountAsync] Found cached account: {_lastAccount.Email}");
            var account = _lastAccount;
            _lastAccount = null; // Clear it so it's only used once
            return account;
        }

        // Otherwise, wait for one to be set
        Console.WriteLine("[GoogleSignInResultHandler.GetAccountAsync] Waiting for account...");
        _accountTcs = new TaskCompletionSource<GoogleSignInAccount?>();
        
        var delay = timeout ?? TimeSpan.FromSeconds(5);
        Console.WriteLine($"[GoogleSignInResultHandler.GetAccountAsync] Timeout: {delay.TotalSeconds} seconds");
        
        using (var cts = new CancellationTokenSource(delay))
        {
            try
            {
                var completedTask = await Task.WhenAny(
                    _accountTcs.Task,
                    Task.Delay(delay, cts.Token)
                );

                if (completedTask == _accountTcs.Task)
                {
                    var account = await _accountTcs.Task;
                    Console.WriteLine($"[GoogleSignInResultHandler.GetAccountAsync] Account received: {(account != null ? account.Email : "null")}");
                    _lastAccount = null; // Clear it
                    return account;
                }
                else
                {
                    // Timeout
                    Console.WriteLine("[GoogleSignInResultHandler.GetAccountAsync] Timeout waiting for account");
                    return null;
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[GoogleSignInResultHandler.GetAccountAsync] Cancelled waiting for account");
                return null;
            }
        }
    }
}
