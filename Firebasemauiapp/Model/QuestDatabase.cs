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

        [ObservableProperty]
        private int currentProgress = 0; // e.g., 1

        [ObservableProperty]
        private int maxProgress = 1; // e.g., 1

        [ObservableProperty]
        private string buttonText = "Go To";

        [ObservableProperty]
        private string buttonColor = "#7CB97C";

        [ObservableProperty]
        private string buttonTextColor = "#FFFFFF";

        [ObservableProperty]
        private string borderColor = "#ACE889";

        [ObservableProperty]
        private double borderOpacity = 1.0;

        [ObservableProperty]
        private string icon = "diary.png"; // Default icon

        public bool IsCompleted => string.Equals(Status, "completed", StringComparison.OrdinalIgnoreCase);

        public string ProgressText => $"{CurrentProgress}/{MaxProgress}";

        public int SortOrder => IsClaimed ? 1 : 0; // Claimed quests go to bottom

        public void UpdateButtonState()
        {
            if (IsClaimed)
            {
                ButtonText = "✓";
                ButtonColor = "#CCCCCC";
                ButtonTextColor = "#FFFFFF";
                BorderColor = "#ACE889";
                BorderOpacity = 0.5; // Faded when claimed
            }
            else if (IsCompleted || CurrentProgress >= MaxProgress)
            {
                ButtonText = "Claim";
                ButtonColor = "#8FB78F";
                ButtonTextColor = "#FFFFFF";
                BorderColor = "#ACE889";
                BorderOpacity = 1.0;
            }
            else
            {
                ButtonText = "Go To";
                ButtonColor = "#FFFFFF";
                ButtonTextColor = "#8FB78F";
                BorderColor = "#ACE889";
                BorderOpacity = 1.0;
            }
        }
    }
}
