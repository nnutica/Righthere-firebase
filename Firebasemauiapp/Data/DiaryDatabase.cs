using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;

namespace Firebasemauiapp.Data;

public class DiaryDatabase
{
    private readonly FirestoreService _firestoreService;
    private readonly string _collectionName = "diaries";

    public DiaryDatabase(FirestoreService firestoreService)
    {
        _firestoreService = firestoreService;
    }

    private async Task<FirestoreDb> GetDatabaseAsync()
    {
        return await _firestoreService.GetDatabaseAsync();
    }
    public async Task<string> SaveDiaryAsync(DiaryData diary)
    {
        try
        {
            var db = await GetDatabaseAsync();

            if (string.IsNullOrEmpty(diary.Id))
            {
                // สร้างใหม่
                diary.Id = Guid.NewGuid().ToString();
                diary.CreatedAt = Timestamp.GetCurrentTimestamp();

                var docRef = db.Collection(_collectionName).Document(diary.Id);
                await docRef.SetAsync(diary);
                return diary.Id;
            }
            else
            {
                // อัปเดต
                var docRef = db.Collection(_collectionName).Document(diary.Id);
                await docRef.SetAsync(diary, SetOptions.MergeAll);
                return diary.Id;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"เกิดข้อผิดพลาดในการบันทึก diary: {ex.Message}");
        }
    }

    public async Task<List<DiaryData>> GetDiariesByUserAsync(string userId)
    {
        try
        {
            var db = await GetDatabaseAsync();

            // แบบที่ 1: ลองใช้ query แบบง่ายก่อน (ไม่มี OrderBy)
            Query query = db.Collection(_collectionName)
                           .WhereEqualTo("userId", userId);

            var snapshot = await query.GetSnapshotAsync();
            var diaries = new List<DiaryData>();

            foreach (var document in snapshot.Documents)
            {
                if (document.Exists)
                {
                    var diary = document.ConvertTo<DiaryData>();
                    diaries.Add(diary);
                }
            }

            // เรียงลำดับใน memory แทนที่จะใช้ Firestore query
            return diaries.OrderByDescending(d => d.CreatedAt.ToDateTime()).ToList();
        }
        catch (Exception ex)
        {
            // ถ้า query แรกไม่ได้ ลองใช้วิธีอื่น
            Console.WriteLine($"Primary query failed: {ex.Message}");

            try
            {
                var db = await GetDatabaseAsync();

                // แบบที่ 2: ดึงทั้งหมดมาแล้วกรองใน memory
                var allDiariesSnapshot = await db.Collection(_collectionName).GetSnapshotAsync();
                var diaries = new List<DiaryData>();

                foreach (var document in allDiariesSnapshot.Documents)
                {
                    if (document.Exists)
                    {
                        var diary = document.ConvertTo<DiaryData>();
                        if (diary.UserId == userId)
                        {
                            diaries.Add(diary);
                        }
                    }
                }

                return diaries.OrderByDescending(d => d.CreatedAt.ToDateTime()).ToList();
            }
            catch (Exception fallbackEx)
            {
                throw new Exception($"เกิดข้อผิดพลาดในการดึงข้อมูล diary: {ex.Message}. Fallback error: {fallbackEx.Message}");
            }
        }
    }

    public async Task<bool> DeleteDiaryAsync(DiaryData diary)
    {
        try
        {
            var db = await GetDatabaseAsync();

            var docRef = db.Collection(_collectionName).Document(diary.Id);
            await docRef.DeleteAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"เกิดข้อผิดพลาดในการลบ diary: {ex.Message}");
        }
    }

}
