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
}
