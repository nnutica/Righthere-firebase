using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Google.Cloud.Firestore;
using Firebasemauiapp.Model;
using Firebasemauiapp.Services;
using Google.Api.Gax;
using System.Threading;

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

    // CHECK if a user has liked a post (stored in subcollection posts/{postId}/likes/{userId})
    public async Task<bool> HasUserLikedAsync(string postId, string userId)
    {
        var db = await GetDatabaseAsync();
        var likeRef = db.Collection(_collectionName).Document(postId).Collection("likes").Document(userId);
        var snap = await likeRef.GetSnapshotAsync();
        return snap.Exists;
    }

    // Try to register a like once per user. Returns true if newly registered and increment applied.
    public async Task<bool> TryLikeOnceAsync(string postId, string userId)
    {
        var db = await GetDatabaseAsync();
        var postRef = db.Collection(_collectionName).Document(postId);
        var likeRef = postRef.Collection("likes").Document(userId);

        // If already liked, do nothing
        var existing = await likeRef.GetSnapshotAsync();
        if (existing.Exists)
            return false;

        // Register like marker for this user
        var payload = new Dictionary<string, object>
        {
            { "userId", userId },
            { "createdAt", Timestamp.FromDateTime(DateTime.UtcNow) }
        };
        await likeRef.SetAsync(payload, SetOptions.Overwrite);

        // Increment like counter with existing resilient method
        var success = await TryIncrementLikesAsync(postId, 1);
        return success;
    }

    // UNLIKE: Remove user's like and decrement counter
    public async Task<bool> UnlikePostAsync(string postId, string userId)
    {
        var db = await GetDatabaseAsync();
        var postRef = db.Collection(_collectionName).Document(postId);
        var likeRef = postRef.Collection("likes").Document(userId);

        // Check if user has liked
        var existing = await likeRef.GetSnapshotAsync();
        if (!existing.Exists)
            return false; // User hasn't liked, nothing to unlike

        // Remove like marker
        await likeRef.DeleteAsync();

        // Decrement like counter
        var success = await TryIncrementLikesAsync(postId, -1);
        return success;
    }

    // DELETE
    public async Task DeletePostAsync(string postId)
    {
        var db = await GetDatabaseAsync();
        var docRef = db.Collection(_collectionName).Document(postId);
        await docRef.DeleteAsync();
    }

    // CHECK if user has posted today
    public async Task<bool> HasUserPostedTodayAsync(string userId)
    {
        try
        {
            Console.WriteLine($"[PostDatabase.HasUserPostedTodayAsync] Called with userId: '{userId}'");
            
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("[PostDatabase] User ID is empty, returning false");
                return false;
            }

            var db = await GetDatabaseAsync();
            
            // Calculate today's date range in UTC
            var today = DateTime.UtcNow.Date;
            var todayStart = today;
            var todayEnd = today.AddDays(1).AddMilliseconds(-1);
            
            var todayStartTimestamp = Timestamp.FromDateTime(todayStart);
            var todayEndTimestamp = Timestamp.FromDateTime(todayEnd);
            
            Console.WriteLine($"[PostDatabase] UTC Date range - Start: {todayStart:yyyy-MM-dd HH:mm:ss}, End: {todayEnd:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"[PostDatabase] Querying for posts where UserId='{userId}'");
            
            // First fetch ALL posts from today (without userId filter) to debug
            var allTodayQuery = db.Collection(_collectionName)
                .WhereGreaterThanOrEqualTo(nameof(PostData.CreatedAt), todayStartTimestamp)
                .WhereLessThanOrEqualTo(nameof(PostData.CreatedAt), todayEndTimestamp);
            
            var allTodaySnapshot = await allTodayQuery.GetSnapshotAsync();
            Console.WriteLine($"[PostDatabase] Total posts from today: {allTodaySnapshot.Count}");
            
            // Then filter by UserId in memory to ensure match
            var userPosts = new List<PostData>();
            foreach (var doc in allTodaySnapshot.Documents)
            {
                if (doc.Exists)
                {
                    try
                    {
                        var post = doc.ConvertTo<PostData>();
                        if (post != null)
                        {
                            Console.WriteLine($"[PostDatabase] Post doc={doc.Id}, UserId='{post.UserId}', Author='{post.Author}', CreatedAt={post.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}");
                            
                            if (post.UserId == userId)
                            {
                                userPosts.Add(post);
                                Console.WriteLine($"[PostDatabase] ✓ MATCH: Found user post: {post.Content?.Substring(0, Math.Min(50, post.Content?.Length ?? 0))}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PostDatabase] Error converting post doc {doc.Id}: {ex.Message}");
                    }
                }
            }
            
            bool hasPosted = userPosts.Count > 0;
            Console.WriteLine($"[PostDatabase] Final result: User '{userId}' has posted today: {hasPosted} (found {userPosts.Count} matching posts)");
            
            return hasPosted;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostDatabase] ERROR in HasUserPostedTodayAsync for user '{userId}': {ex.Message}");
            Console.WriteLine($"[PostDatabase] Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}
