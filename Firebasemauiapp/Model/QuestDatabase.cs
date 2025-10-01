using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Google.Cloud.Firestore;

namespace Firebasemauiapp.Model
{
    [FirestoreData]
    public partial class QuestDatabase : ObservableObject
    {
        [FirestoreProperty("questId")]
        public string QuestID { get; set; } = string.Empty;

        [FirestoreProperty("title")]
        public string Title { get; set; } = string.Empty;

        // e.g., "Daily"
        [FirestoreProperty("type")]
        public string Type { get; set; } = "Daily";

        [FirestoreProperty("startAt")]
        public Timestamp StartAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

        // e.g., "pending" | "completed"
        [FirestoreProperty("status")]
        public string Status { get; set; } = "pending";

        // coin reward
        [FirestoreProperty("reward")]
        public int Reward { get; set; } = 0;

        [FirestoreProperty("endAt")]
        public Timestamp EndAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

        // UI-only fields (not mapped)
        [ObservableProperty]
        private int progress; // 0..100

        [ObservableProperty]
        private bool isClaimed; // UI-only: whether reward claimed

        public bool CanClaim => string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase) && !IsClaimed;

        public bool IsCompleted => string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase);
    }
}
