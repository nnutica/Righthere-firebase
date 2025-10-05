using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Google.Cloud.Firestore;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;
using Google.Api.Gax;

namespace Firebasemauiapp.Data;

public class PostDatabase
{
    private readonly FirestoreService _firestoreService;
    private readonly string _collectionName = "posts";

    public PostDatabase(FirestoreService firestoreService)
    {
        _firestoreService = firestoreService;
    }

    private async Task<FirestoreDb> GetDatabaseAsync()
    {
        return await _firestoreService.GetDatabaseAsync();
    }

    // CREATE
    public async Task<string> CreatePostAsync(PostData post)
    {
        var db = await GetDatabaseAsync();
        if (string.IsNullOrEmpty(post.PostId))
        {
            post.PostId = Guid.NewGuid().ToString();
        }
        post.CreatedAt = DateTime.UtcNow;
        var docRef = db.Collection(_collectionName).Document(post.PostId);
        await docRef.SetAsync(post);
        return post.PostId;
    }

    // READ (Get by Id)
    public async Task<PostData?> GetPostByIdAsync(string postId)
    {
        var db = await GetDatabaseAsync();
        var docRef = db.Collection(_collectionName).Document(postId);
        var snapshot = await docRef.GetSnapshotAsync();
        if (snapshot.Exists)
        {
            var post = snapshot.ConvertTo<PostData>();
            // Always sync PostId to document id
            if (post != null)
            {
                post.PostId = snapshot.Id;
            }
            return post;
        }
        return null;
    }

    // READ (Get by PostId field value)
    public async Task<PostData?> GetPostByPostIdFieldAsync(string postId)
    {
        var db = await GetDatabaseAsync();
        var query = db.Collection(_collectionName).WhereEqualTo(nameof(PostData.PostId), postId).Limit(1);
        var snapshot = await query.GetSnapshotAsync();
        var doc = snapshot.Documents.FirstOrDefault();
        if (doc != null && doc.Exists)
        {
            var post = doc.ConvertTo<PostData>();
            if (post != null)
            {
                // Always sync PostId to doc id to avoid mismatch
                post.PostId = doc.Id;
            }
            return post;
        }
        return null;
    }

    // READ (Get all)
    public async Task<List<PostData>> GetAllPostsAsync()
    {
        var db = await GetDatabaseAsync();
        var query = db.Collection(_collectionName);
        var snapshot = await query.GetSnapshotAsync();
        var posts = new List<PostData>();
        foreach (var doc in snapshot.Documents)
        {
            if (doc.Exists)
            {
                var post = doc.ConvertTo<PostData>();
                if (post != null)
                {
                    // Always sync PostId to doc id to avoid mismatch
                    post.PostId = doc.Id;
                    posts.Add(post);
                }
            }
        }
        return posts;
    }

    // UPDATE
    public async Task UpdatePostAsync(PostData post)
    {
        var db = await GetDatabaseAsync();
        if (string.IsNullOrEmpty(post.PostId))
            throw new ArgumentException("PostId is required for update.");
        var docRef = db.Collection(_collectionName).Document(post.PostId);
        await docRef.SetAsync(post, SetOptions.MergeAll);
    }

    // Atomic increment for Likes with fallback when document id != PostId field
    public async Task<bool> TryIncrementLikesAsync(string postId, int delta = 1)
    {
        Console.WriteLine($"TryIncrementLikesAsync: Attempting to increment likes for postId: {postId}");
        var db = await GetDatabaseAsync();
        var docRef = db.Collection(_collectionName).Document(postId);
        try
        {
            await docRef.UpdateAsync(new Dictionary<string, object>
            {
                { nameof(PostData.Likes), FieldValue.Increment(delta) }
            });
            Console.WriteLine($"TryIncrementLikesAsync: Successfully incremented likes for {postId}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TryIncrementLikesAsync: Direct update failed: {ex.Message}");
            // If the document with id=postId doesn't exist, try finding by field PostId
            var query = db.Collection(_collectionName).WhereEqualTo(nameof(PostData.PostId), postId).Limit(1);
            var snapshot = await query.GetSnapshotAsync();
            var doc = snapshot.Documents.FirstOrDefault();
            if (doc == null)
            {
                Console.WriteLine($"TryIncrementLikesAsync: No document found with PostId field = {postId}");
                return false;
            }
            await doc.Reference.UpdateAsync(new Dictionary<string, object>
            {
                { nameof(PostData.Likes), FieldValue.Increment(delta) }
            });
            Console.WriteLine($"TryIncrementLikesAsync: Successfully incremented via fallback query for {postId}");
            return true;
        }
    }

    // DELETE
    public async Task DeletePostAsync(string postId)
    {
        var db = await GetDatabaseAsync();
        var docRef = db.Collection(_collectionName).Document(postId);
        await docRef.DeleteAsync();
    }
}
