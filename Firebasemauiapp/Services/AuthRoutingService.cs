using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Microsoft.Maui.ApplicationModel;

namespace Firebasemauiapp.Services
{
    public class AuthRoutingService
    {
        private readonly FirebaseAuthClient _auth;
        private bool _started;
        private bool _disposed;

        public AuthRoutingService(FirebaseAuthClient auth)
        {
            _auth = auth;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            _auth.AuthStateChanged += OnAuthStateChanged;

            // Route once on startup based on current state
            _ = RouteForCurrentStateAsync();
        }

        private async void OnAuthStateChanged(object? sender, UserEventArgs e)
        {
            if (_disposed) return;
            await RouteForCurrentStateAsync();
        }

        private async Task RouteForCurrentStateAsync()
        {
            if (_disposed) return;

            try
            {
                // Ensure Shell is ready
                for (int i = 0; i < 40 && Shell.Current == null; i++)
                {
                    if (_disposed) return;
                    await Task.Delay(50);
                }

                if (Shell.Current != null && !_disposed)
                {
                    var current = Shell.Current.CurrentState?.Location?.ToString() ?? string.Empty;

                    // Avoid jumping to Starter while user is on SignUp page
                    if (_auth.User != null && current.Contains("signup", StringComparison.OrdinalIgnoreCase))
                        return;

                    var route = _auth.User != null ? "//starter" : "//signin";

                    if (!_disposed && Shell.Current != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            if (!_disposed && Shell.Current != null)
                                await Shell.Current.GoToAsync(route);
                        });
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                _disposed = true;
            }
            catch
            {
                // no-op
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _auth.AuthStateChanged -= OnAuthStateChanged;
        }
    }
}
