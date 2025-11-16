using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.Model
{
    [FirestoreData]
    public partial class DiaryData : ObservableObject
    {
        [FirestoreProperty("id")]
        public string Id { get; set; } = string.Empty;

        [FirestoreProperty("userId")]
        public string UserId { get; set; } = string.Empty;



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

        // Backward-compatible single image field
        [FirestoreProperty("imageUrl")]
        public string? ImageUrl { get; set; }

        // New: multiple images support
        [FirestoreProperty("imageUrls")]
        public List<string>? ImageUrls { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        // Helper property for easier DateTime handling
        public DateTime CreatedAtDateTime
        {
            get => CreatedAt.ToDateTime();
            set => CreatedAt = Timestamp.FromDateTime(value.ToUniversalTime());
        }

        // UI property: SeeMore/SeeLess toggle (not mapped to Firestore)
        [ObservableProperty]
        private bool isExpanded = false;

        // คืนชื่อไฟล์รูปภาพตามอารมณ์ (Mood)
        public string MoodImage
        {
            get
            {
                return Mood?.ToLower() switch
                {
                    "joy" => "joy.png",
                    "anger" => "anger.png",
                    "sadness" => "sadness.png",
                    "fear" => "fear.png",
                    "love" => "love.png",
                    "surprise" => "surprise.png",
                    _ => "empty.png"
                };
            }
        }
    }
}
