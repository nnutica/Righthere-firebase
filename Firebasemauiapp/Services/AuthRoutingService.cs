using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Auth;
using Google.Cloud.Firestore;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Firebasemauiapp.Services
{
    public class AuthRoutingService : IDisposable
    {
        private readonly FirebaseAuthClient _auth;
        private readonly FirestoreService _firestoreService;
        private bool _started;
        private bool _disposed;
        private string? _lastRoutedState;

        public AuthRoutingService(FirebaseAuthClient auth, FirestoreService firestoreService)
        {
            _auth = auth;
            _firestoreService = firestoreService;
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            _auth.AuthStateChanged += OnAuthStateChanged;

            // Initial route
            _ = RouteWhenReadyAsync();
        }

        private void OnAuthStateChanged(object? sender, UserEventArgs e)
        {
            if (_disposed) return;
            _ = RouteWhenReadyAsync();
        }

        private async Task RouteWhenReadyAsync()
        {
            try
            {
                // 1️⃣ รอ Shell พร้อม
                for (int i = 0; i < 60 && Shell.Current == null; i++)
                {
                    if (_disposed) return;
                    await Task.Delay(50);
                }

                if (_disposed || Shell.Current == null)
                    return;

                // 2️⃣ รอ Firebase Auth restore (เครื่องจริงช้ากว่า emulator)
                for (int i = 0; i < 20; i++)
                {
                    if (_disposed) return;

                    if (_auth.User != null || i >= 10)
                        break;

                    await Task.Delay(100);
                }

                if (_disposed || Shell.Current == null)
                    return;

                var isLoggedIn = _auth.User != null;
                var targetState = isLoggedIn ? "AUTHENTICATED" : "UNAUTHENTICATED";

                // 3️⃣ route เฉพาะตอน state เปลี่ยน
                if (_lastRoutedState == targetState)
                    return;

                _lastRoutedState = targetState;

                if (isLoggedIn)
                {
                    await UpdateLastActiveAsync();
                }

                var route = isLoggedIn ? "//starter" : "//signin";

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (_disposed || Shell.Current == null) return;
                    await Shell.Current.GoToAsync(route, animate: false);
                });
            }
            catch (ObjectDisposedException)
            {
                _disposed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthRoutingService error: {ex.Message}");
            }
        }

        private async Task UpdateLastActiveAsync()
        {
            var uid = _auth.User?.Uid;
            if (string.IsNullOrWhiteSpace(uid))
                return;

            try
            {
                var db = await _firestoreService.GetDatabaseAsync();
                var userDocRef = db.Collection("users").Document(uid);
                var now = Timestamp.FromDateTime(DateTime.UtcNow);
                await userDocRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "lastActiveAt", now }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update lastActiveAt error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _auth.AuthStateChanged -= OnAuthStateChanged;
        }
    }
}
