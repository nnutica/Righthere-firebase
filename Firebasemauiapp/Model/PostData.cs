using System;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.Model;

[FirestoreData]
public class PostData
{
    [FirestoreProperty]
    public string PostId { get; set; }

    [FirestoreProperty]
    public string Content { get; set; }

    [FirestoreProperty]
    public string Author { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty]
    public int Likes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; }

    // Post-it appearance properties
    [FirestoreProperty]
    public string PostItColor { get; set; }

    [FirestoreProperty]
    public string TextColor { get; set; }
}
