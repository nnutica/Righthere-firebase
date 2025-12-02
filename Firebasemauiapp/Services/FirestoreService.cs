using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;

namespace Firebasemauiapp.Services;

public class FirestoreService
{
    private FirestoreDb? db;

    public FirestoreService()
    {
        _ = InitializeFirestoreAsync();
    }

    private async Task InitializeFirestoreAsync()
    {
        try
        {
            Console.WriteLine("FirestoreService: Starting initialization...");
            Console.WriteLine($"FirestoreService: App data directory: {FileSystem.AppDataDirectory}");
            Console.WriteLine($"FirestoreService: Cache directory: {FileSystem.CacheDirectory}");

            // Method 1: อ่านจาก EmbeddedResource (จะทำงานทั้ง emulator และ deployed APK)
            try
            {
                Console.WriteLine("FirestoreService: Method 1 - Reading admin-sdk.json from EmbeddedResource...");

                var assembly = typeof(FirestoreService).Assembly;
                var resourceNames = assembly.GetManifestResourceNames();
                Console.WriteLine($"FirestoreService: Available embedded resources: {string.Join(", ", resourceNames)}");

                // ลองหาชื่อ resource ที่มี admin-sdk
                var resourceName = resourceNames.FirstOrDefault(n => n.Contains("admin-sdk"));

                if (resourceName == null)
                {
                    // ลองใช้ชื่อที่เรากำหนดใน LogicalName
                    resourceName = "admin-sdk.json";
                    Console.WriteLine($"FirestoreService: Trying LogicalName: {resourceName}");
                    
                    // ถ้ายังไม่เจอ ลองชื่อเต็มแบบ namespace
                    if (!resourceNames.Contains(resourceName))
                    {
                        resourceName = "Firebasemauiapp.Resources.Raw.admin-sdk.json";
                        Console.WriteLine($"FirestoreService: Trying full path: {resourceName}");
                    }
                }
                else
                {
                    Console.WriteLine($"FirestoreService: Found resource: {resourceName}");
                }

                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    throw new FileNotFoundException($"Embedded resource '{resourceName}' not found in assembly");
                }

                Console.WriteLine($"FirestoreService: ✅ Resource stream opened! Length: {stream.Length} bytes");

                var credential = GoogleCredential.FromStream(stream);
                Console.WriteLine("FirestoreService: ✅ Credential created from embedded resource");

                var projectId = "righthere-backend";
                var builder = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    Credential = credential
                };

                Console.WriteLine("FirestoreService: Building Firestore DB...");
                db = await builder.BuildAsync();
                Console.WriteLine("FirestoreService: ✅✅✅ Initialized with admin-sdk.json from EmbeddedResource!");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FirestoreService: ❌ Method 1 failed: {ex.GetType().Name}");
                Console.WriteLine($"FirestoreService: Message: {ex.Message}");
                Console.WriteLine($"FirestoreService: StackTrace: {ex.StackTrace}");
            }

            // Method 2: ลองอ่านจาก FileSystem (fallback สำหรับ emulator)
            try
            {
                Console.WriteLine("FirestoreService: Method 2 - Trying FileSystem.OpenAppPackageFileAsync...");
                using var stream = await FileSystem.Current.OpenAppPackageFileAsync("admin-sdk.json");

                Console.WriteLine($"FirestoreService: ✅ Stream opened from FileSystem! Length: {stream.Length} bytes");

                var credential = GoogleCredential.FromStream(stream);
                var projectId = "righthere-backend";
                var builder = new FirestoreDbBuilder
                {
                    ProjectId = projectId,
                    Credential = credential
                };

                db = await builder.BuildAsync();
                Console.WriteLine("FirestoreService: ✅ Initialized with FileSystem method!");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FirestoreService: ❌ Method 2 failed: {ex.Message}");
            }

            // Fallback: ลองใช้ environment variables (จะไม่สำเร็จใน Android แต่ลองดู)
            Console.WriteLine("FirestoreService: ⚠️ Attempting fallback with environment credentials...");

            try
            {
                // ลองสร้างแบบใช้ default credentials (ไม่น่าจะได้บน Android)
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                    Path.Combine(FileSystem.AppDataDirectory, "admin-sdk.json"));

                db = FirestoreDb.Create("righthere-backend");
                Console.WriteLine("FirestoreService: ⚠️ Initialized with environment credentials");
            }
            catch (Exception envEx)
            {
                Console.WriteLine($"FirestoreService: ❌ Environment credentials failed: {envEx.Message}");

                // Last resort: สร้าง anonymous GoogleCredential
                Console.WriteLine("FirestoreService: 🔧 Creating anonymous credential as last resort...");

                var anonCredential = GoogleCredential.FromAccessToken(null);
                var builder = new FirestoreDbBuilder
                {
                    ProjectId = "righthere-backend",
                    Credential = anonCredential
                };

                db = await builder.BuildAsync();
                Console.WriteLine("FirestoreService: ⚠️ Created with anonymous credential - ACCESS WILL BE LIMITED");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FirestoreService: ❌ FATAL: Initialization failed completely");
            Console.WriteLine($"FirestoreService: Exception type: {ex.GetType().Name}");
            Console.WriteLine($"FirestoreService: Message: {ex.Message}");
            Console.WriteLine($"FirestoreService: StackTrace: {ex.StackTrace}");
            throw new Exception($"Unable to initialize Firestore: {ex.Message}", ex);
        }
    }

    public FirestoreDb? GetDatabase()
    {
        return db;
    }

    public async Task<FirestoreDb> GetDatabaseAsync()
    {
        // รอจนกว่า initialization จะเสร็จ
        int attempts = 0;
        while (db == null && attempts < 50) // รอไม่เกิน 5 วินาที
        {
            await Task.Delay(100);
            attempts++;
        }

        if (db == null)
        {
            throw new Exception("Firestore database is not initialized. Please check your configuration.");
        }

        Console.WriteLine("Firestore database is ready for use");
        return db;
    }
}
