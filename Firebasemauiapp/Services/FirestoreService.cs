using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using System.Text;

namespace Firebasemauiapp.Services;

public class FirestoreService
{
    private FirestoreDb? _db;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized = false;
    private Exception? _initializationException;

    public FirestoreService()
    {
        // ไม่ต้อง fire-and-forget อีกต่อไป
        // ให้เรียก GetDatabaseAsync() ตอนที่จะใช้งานจริง
    }

    private async Task InitializeAsync()
    {
        await _initializationLock.WaitAsync();
        try
        {
            if (_isInitialized)
            {
                return;
            }

            Console.WriteLine("=== FirestoreService: Starting Initialization ===");
            Console.WriteLine($"Platform: {DeviceInfo.Platform}");
            Console.WriteLine($"AppDataDirectory: {FileSystem.AppDataDirectory}");

            GoogleCredential? credential = null;
            const string projectId = "righthere-backend";

            // วิธีที่ 1: ลองโหลดจาก embedded resource (MAUI Asset)
            try
            {
                Console.WriteLine("Attempting to load from MAUI asset...");
                using var stream = await FileSystem.OpenAppPackageFileAsync("admin-sdk.json");

                if (stream == null || stream.Length == 0)
                {
                    throw new InvalidOperationException("Stream is null or empty");
                }

                Console.WriteLine($"Stream length: {stream.Length} bytes");
                credential = GoogleCredential.FromStream(stream);
                Console.WriteLine("✅ Successfully loaded credential from MAUI asset");
            }
            catch (Exception ex1)
            {
                Console.WriteLine($"Failed to load from MAUI asset: {ex1.Message}");

                // วิธีที่ 2: สร้าง credential จาก JSON string โดยตรง (สำหรับ Android/iOS)
                try
                {
                    Console.WriteLine("Attempting to load from embedded JSON string...");

                    string jsonContent = @"{
  ""type"": ""service_account"",
  ""project_id"": ""righthere-backend"",
  ""private_key_id"": ""b564f0e44017fd1e8561d5e77892d935073d7212"",
  ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQC5pe4/cakflPHa\nNJiTwaRqWpf/QT7TOgWHtLbC02eqWobR/haVTWK4EZ0+58Azto+tgw1oaSSiRg1Q\nBq+jbkZABzYETu4YWQKJu48csQv8xDb8c8dz9w2HYq2Cbjt8U8vTwnwPP3uTbH8m\nD57uIrvIfVBwcjVHKBZtGDWk3vLKNFmW58yyXsL3cVAVXhvcl8zPFUuFnJ2I/D3J\n0BEhKCc/FWC69rvXyQAgubzptlHd4zBTzDhaQ03EFyEWbK3a7dXQOAo7EI8tJ628\n84kzK0jJ8S759yuP+qBNyW/xQeHcFz111Nbd/bceighJDMD0KgeYph05Nxs+PlPR\n24gzWCk/AgMBAAECggEACySznU+xC1OnnljQqiXSCadwcjW0LX4ESo+e9lwCkye/\ng44d3hssCGJxe6iLhLWGzRWnUcbl5Qh30H+zCtTmsJcxaqbJa3q3e675Blh2OldI\nxnSCQPeDJccKWGCBGS0qeFNQzdWAqtCByhx4uYPD0JZjcRLZ5QogXOIHVK/xOncG\nZKz9XttgwdQe6BmwAc62VbkqT81L3tvASdOZbmsRq4LJtrBqBs/3N2UN6Ah3y8JV\nii11b4iH4souVwl1CXrE6ywhF2uJaHMsl/vWiM8vBxh63WmTjJBbdluJ1E2vmSMC\ntaO9M+NAdcUNRMMnen1sFoRSGc3F26HOKsbOCwiuwQKBgQD2wYGgrVeYdCK3zbco\nN309dnOWd1iPeB/UkkOV7q9KpSj0E7guo5qjSlHi1JamDL9+r3BnZDlKNklW/nmc\nuRR+BKx5fl0yvjgMnG4FV6ZgHYlakU4zhkqjvWASStWZZuv7QVpPsaKNP7gWTen5\npL7RBqxQo1Id4DzvPTMAEwAEoQKBgQDAmmAXOyLp+2vOPYog7q9hwD2kfT9nWr9+\nhk/xXOVTjOQvH9zgXtvvKMaLnrYTRk8ECQhs+i4hYJwqkARLZK7FCYV7p8vKBh/I\nl6zuWLGTSYiXkZ3ohwGAFx74eocMkoSlORHk+Tphky4oKcgVpab+jk4L9mLK5/pG\nBY5HRJ+B3wKBgFaks8Obmjpp7RblIP76HPvL7+JRncMixup5QUoQOXTYcXziv7WA\ntPfJTN99DjjYGRV+vNVRF9y7Gx101Xb2df+Z3IX8nPUIXd3vv6IYmM7/EA/BHdhx\nuxurj2Rc6oum3A2pcPCyywUV7qnGSfXipy32TeMytc7PwhWvQ40vHr6BAoGBAIMQ\nSjVky5R3v9u/quBQLE6TmB74EA5QBaGe2oW/llqttJWQ1ChmxLlRgRJ/tR0Wqixv\nzSkDciLKcFrSV+nKINf6a7hC7f2S/0vsUwR6nJRC5M1njRIv1MiKc0vZbU8T4Wnh\nlRjAtIaztiQkkoCQkjwFMH0ZdA7pnzjJDRHYU8b3AoGBAJUrzZ0kzS2M4lhORAyU\n86JaS/slKX/qeJw+cpTR2I79yi2pVlbkluUPIj7YuRB40byXTCJU53YZEetMMbdd\n6N6GqdLviIeKRCKSLy41QKgiVb95VZwTQjUuLArF9rzYg9osAagyTT+iAJ3LaoAH\nbG5TKCUGw6EPu3MWgfLup/U3\n-----END PRIVATE KEY-----\n"",
  ""client_email"": ""firebase-adminsdk-fbsvc@righthere-backend.iam.gserviceaccount.com"",
  ""client_id"": ""107374503722309684939"",
  ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
  ""token_uri"": ""https://oauth2.googleapis.com/token"",
  ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
  ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-fbsvc%40righthere-backend.iam.gserviceaccount.com"",
  ""universe_domain"": ""googleapis.com""
}";

                    var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
                    using var jsonStream = new MemoryStream(jsonBytes);
                    credential = GoogleCredential.FromStream(jsonStream);
                    Console.WriteLine("✅ Successfully loaded credential from embedded JSON");
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"Failed to load from embedded JSON: {ex2.Message}");

                    // วิธีที่ 3: สำหรับ Windows development
                    if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    {
                        Console.WriteLine("Attempting Windows fallback paths...");

                        var paths = new[]
                        {
                            @"d:\Firebasemauiapp\Firebasemauiapp\Resources\Raw\admin-sdk.json",
                            Path.Combine(AppContext.BaseDirectory, "Resources", "Raw", "admin-sdk.json"),
                            Path.Combine(Environment.CurrentDirectory, "Resources", "Raw", "admin-sdk.json")
                        };

                        bool found = false;
                        foreach (var path in paths)
                        {
                            if (File.Exists(path))
                            {
                                Console.WriteLine($"Found at: {path}");
                                credential = GoogleCredential.FromFile(path);
                                Console.WriteLine("✅ Successfully loaded credential from file");
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            throw new FileNotFoundException(
                                "Cannot find admin-sdk.json. Tried:\n" + string.Join("\n", paths),
                                "admin-sdk.json"
                            );
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Failed to load Firebase credentials. All methods failed.",
                            ex2
                        );
                    }
                }
            }

            // ตรวจสอบว่า credential ถูกโหลดสำเร็จแล้ว
            if (credential == null)
            {
                throw new InvalidOperationException("Failed to load Firebase credentials from any source.");
            }

            // สร้าง FirestoreDb
            Console.WriteLine($"Creating FirestoreDb with project: {projectId}");
            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = credential
            };

            _db = await builder.BuildAsync();
            _isInitialized = true;
            Console.WriteLine("✅✅✅ Firestore initialized successfully!");
        }
        catch (Exception ex)
        {
            _initializationException = ex;
            Console.WriteLine($"❌ FATAL: Firestore initialization failed");
            Console.WriteLine($"Error: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    public async Task<FirestoreDb> GetDatabaseAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        if (_initializationException != null)
        {
            throw new InvalidOperationException(
                "Firestore failed to initialize previously. Check logs for details.",
                _initializationException
            );
        }

        if (_db == null)
        {
            throw new InvalidOperationException("Firestore database is null after initialization.");
        }

        return _db;
    }
}
