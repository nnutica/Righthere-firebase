using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;

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
            return snapshot.ConvertTo<PostData>();
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
                posts.Add(doc.ConvertTo<PostData>());
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

    // DELETE
    public async Task DeletePostAsync(string postId)
    {
        var db = await GetDatabaseAsync();
        var docRef = db.Collection(_collectionName).Document(postId);
        await docRef.DeleteAsync();
    }
}
