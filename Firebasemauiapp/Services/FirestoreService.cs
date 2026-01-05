using System;
using System.IO;
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
            // อ่าน service account key จากไฟล์ admin-sdk.json ใน Resources/Raw
            using var stream = await FileSystem.Current.OpenAppPackageFileAsync("admin-sdk.json");

            // สร้าง credential จาก stream
            var credential = GoogleCredential.FromStream(stream);

            // สร้าง FirestoreDb ด้วย credential
            var projectId = "righthere-backend";

            // สร้าง FirestoreDbBuilder และตั้งค่า credential
            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                Credential = credential
            };

            db = await builder.BuildAsync();

            Console.WriteLine("Firestore initialized successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Firestore: {ex.Message}");
            // สำหรับ development ถ้า credential ไม่ทำงาน ให้ลองใช้แบบง่าย
            try
            {
                db = FirestoreDb.Create("righthere-backend");
                Console.WriteLine("Firestore initialized with default credentials");
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"Fallback initialization also failed: {ex2.Message}");
                throw;
            }
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

    /// <summary>
    /// ดึงข้อมูล user จาก Firestore users collection
    /// </summary>
    public async Task<DocumentSnapshot> GetUserDataAsync(string uid)
    {
        try
        {
            var database = await GetDatabaseAsync();
            var userDocRef = database.Collection("users").Document(uid);
            var snapshot = await userDocRef.GetSnapshotAsync();
            return snapshot;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user data: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// ดึง username จาก Firestore
    /// </summary>
    public async Task<string> GetUsernameAsync(string uid)
    {
        try
        {
            var snapshot = await GetUserDataAsync(uid);
            if (snapshot.Exists && snapshot.TryGetValue<string>("username", out var username))
            {
                return username;
            }
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// ดึง coin จาก Firestore
    /// </summary>
    public async Task<int> GetCoinAsync(string uid)
    {
        try
        {
            var snapshot = await GetUserDataAsync(uid);
            if (snapshot.Exists && snapshot.TryGetValue<int>("coin", out var coin))
            {
                return coin;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// ดึง plant และ pot จาก Firestore
    /// </summary>
    public async Task<(string plant, string pot)> GetPlantAndPotAsync(string uid)
    {
        try
        {
            var snapshot = await GetUserDataAsync(uid);
            if (snapshot.Exists)
            {
                var plant = snapshot.TryGetValue<string>("currentPlant", out var p) ? p : "empty.png";
                var pot = snapshot.TryGetValue<string>("currentPot", out var pt) ? pt : "pot.png";
                return (plant, pot);
            }
            return ("empty.png", "pot.png");
        }
        catch
        {
            return ("empty.png", "pot.png");
        }
    }

    /// <summary>
    /// Deletes all user data from Firestore (Documents in 'users' and 'diaries')
    /// </summary>
    public async Task DeleteUserDataAsync(string uid)
    {
        try
        {
            var database = await GetDatabaseAsync();

            // 1. Delete User Profile
            var userDocRef = database.Collection("users").Document(uid);
            await userDocRef.DeleteAsync();
            Console.WriteLine($"[FirestoreService] Deleted user profile for {uid}");

            // 2. Delete All Diaries for this user
            // Note: In production with many documents, this should be done differently (e.g. valid batching or cloud function)
            // But for this scope, a client-side query and batch delete is acceptable.
            var diariesQuery = database.Collection("diaries").WhereEqualTo("userId", uid);
            var diariesSnapshot = await diariesQuery.GetSnapshotAsync();

            if (diariesSnapshot.Count > 0)
            {
                var batch = database.StartBatch();
                foreach (var doc in diariesSnapshot.Documents)
                {
                    batch.Delete(doc.Reference);
                }
                await batch.CommitAsync();
                Console.WriteLine($"[FirestoreService] Deleted {diariesSnapshot.Count} diaries for {uid}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FirestoreService] Error deleting user data: {ex.Message}");
            throw; 
        }
    }
}
