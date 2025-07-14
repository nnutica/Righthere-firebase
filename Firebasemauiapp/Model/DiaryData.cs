using System;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.Model;

[FirestoreData]
public class DiaryData
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("userId")]
    public string UserId { get; set; } = string.Empty;

    [FirestoreProperty("reason")]
    public string Reason { get; set; } = string.Empty;

    [FirestoreProperty("content")]
    public string Content { get; set; } = string.Empty;

    [FirestoreProperty("mood")]
    public string Mood { get; set; } = string.Empty;

    [FirestoreProperty("sentimentScore")]
    public double SentimentScore { get; set; }

    [FirestoreProperty("suggestion")]
    public string Suggestion { get; set; } = string.Empty;

    [FirestoreProperty("keywords")]
    public string Keywords { get; set; } = string.Empty;

    [FirestoreProperty("emotionalReflection")]
    public string EmotionalReflection { get; set; } = string.Empty;

    [FirestoreProperty("createdAt")]
    public Timestamp CreatedAt { get; set; }

    // Helper property for easier DateTime handling
    public DateTime CreatedAtDateTime
    {
        get => CreatedAt.ToDateTime();
        set => CreatedAt = Timestamp.FromDateTime(value.ToUniversalTime());
    }
}
