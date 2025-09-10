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
    public int Likes { get; set; }

    [FirestoreProperty]
    public DateTime CreatedAt { get; set; }
}
