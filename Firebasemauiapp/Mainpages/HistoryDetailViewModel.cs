using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebasemauiapp.Model;
using Microsoft.Maui.Graphics;

namespace Firebasemauiapp.Mainpages;

public partial class HistoryDetailViewModel : ObservableObject
{
    [ObservableProperty]
    private string _mood = string.Empty;

    [ObservableProperty]
    private string _suggestion = string.Empty;

    [ObservableProperty]
    private string _keywords = string.Empty;

    [ObservableProperty]
    private string _emotion = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _score = string.Empty;

    [ObservableProperty]
    private string? _imageUrl;

    [ObservableProperty]
    private string _intensityText = "A Little Bit";

    [ObservableProperty]
    private string _moodIntensityLabel = "Happiness A Little Bit";

    [ObservableProperty]
    private ImageSource? _emotionImage;

    // Paging state for 4-step view
    [ObservableProperty]
    private int _pageIndex = 0; // 0: DiaryContent, 1: KeyThemes, 2: Reflection, 3: Suggestion

    public bool IsFirstPage => PageIndex == 0;
    public bool IsLastPage => PageIndex == 3;

    [ObservableProperty]
    private ObservableCollection<string> _keywordsList = new();

    // Individual keyword properties for Post-it binding
    [ObservableProperty]
    private string _keyword1 = string.Empty;

    [ObservableProperty]
    private string _keyword2 = string.Empty;

    [ObservableProperty]
    private string _keyword3 = string.Empty;

    [ObservableProperty]
    private string _keyword4 = string.Empty;

    [ObservableProperty]
    private string _keyword5 = string.Empty;

    // Dynamic positioned keyword cards
    [ObservableProperty]
    private ObservableCollection<KeywordCard> _keywordCards = new();

    public HistoryDetailViewModel()
    {
        PreviousPageCommand = new RelayCommand(PreviousPage);
        NextPageCommand = new RelayCommand(NextPage);
        CloseCommand = new AsyncRelayCommand(Close);
    }

    public RelayCommand PreviousPageCommand { get; }
    public RelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand CloseCommand { get; }

    public void SetData(DiaryData diary)
    {
        if (diary == null) return;

        Content = diary.Content ?? string.Empty;
        Mood = diary.Mood ?? string.Empty;
        Suggestion = diary.Suggestion ?? string.Empty;
        Keywords = diary.Keywords ?? string.Empty;
        Emotion = diary.EmotionalReflection ?? string.Empty;
        Score = diary.SentimentScore.ToString();
        IntensityText = ""; // You can add intensity if needed
        MoodIntensityLabel = Mood;
        ImageUrl = diary.ImageUrl;

        SetEmotionImage(Mood);
        BuildKeywordsList(Keywords);

        // Reset to first page
        PageIndex = 0;
        OnPropertyChanged(nameof(IsFirstPage));
        OnPropertyChanged(nameof(IsLastPage));
    }

    private void BuildKeywordsList(string keywords)
    {
        try
        {
            var parts = (keywords ?? string.Empty)
                .Split(new[] { ',', '\n', '\r', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            KeywordsList = new ObservableCollection<string>();
            KeywordCards = new ObservableCollection<KeywordCard>();
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (!string.IsNullOrWhiteSpace(t)) KeywordsList.Add(t);
            }

            // Update individual keyword properties
            Keyword1 = KeywordsList.Count > 0 ? KeywordsList[0] : string.Empty;
            Keyword2 = KeywordsList.Count > 1 ? KeywordsList[1] : string.Empty;
            Keyword3 = KeywordsList.Count > 2 ? KeywordsList[2] : string.Empty;
            Keyword4 = KeywordsList.Count > 3 ? KeywordsList[3] : string.Empty;
            Keyword5 = KeywordsList.Count > 4 ? KeywordsList[4] : string.Empty;

            BuildKeywordCards();
        }
        catch { KeywordsList = new ObservableCollection<string>(); }
    }

    private void BuildKeywordCards()
    {
        KeywordCards.Clear();
        if (KeywordsList.Count == 0) return;

        var colors = new[] { "#A6C9C6", "#F0E7DA", "#8EB7D1", "#25E88C", "#F4F3EF", "#D7C1FF", "#FFD9A1" };
        var rects = new List<Rect>
        {
            new Rect(0.62, 0.04, 240, 240),
            new Rect(0.38, 0.18, 240, 270),
            new Rect(0.62, 0.31, 240, 240),
            new Rect(0.35, 0.48, 240, 240),
            new Rect(0.65, 0.63, 240, 100)
        };

        int count = Math.Min(KeywordsList.Count, rects.Count);
        for (int i = 0; i < count; i++)
        {
            KeywordCards.Add(new KeywordCard
            {
                Text = KeywordsList[i],
                Bounds = rects[i],
                Color = colors[i % colors.Length]
            });
        }

        if (KeywordsList.Count > rects.Count)
        {
            double startY = 0.78;
            double rowHeight = 0.10;
            double chipH = 0.08;
            double xStart = 0.05;
            double x = xStart;
            foreach (var extra in KeywordsList.Skip(rects.Count))
            {
                double chipW = 0.28;
                if (x + chipW > 0.95)
                {
                    x = xStart;
                    startY += rowHeight;
                    if (startY > 0.92) break;
                }
                KeywordCards.Add(new KeywordCard
                {
                    Text = extra,
                    Bounds = new Rect(x, startY, chipW, chipH),
                    Color = colors[(KeywordCards.Count) % colors.Length]
                });
                x += chipW + 0.02;
            }
        }
    }

    private void SetEmotionImage(string mood)
    {
        var imageName = mood.ToLower() switch
        {
            "happiness" => "happiness.png",
            "joy" => "happiness.png",
            "anger" => "anger.png",
            "sadness" => "sadness.png",
            "fear" => "fear.png",
            "love" => "love.png",
            "surprise" => "surprise.png",
            _ => "empty.png"
        };
        EmotionImage = ImageSource.FromFile(imageName);
    }

    private void PreviousPage()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            OnPropertyChanged(nameof(IsFirstPage));
            OnPropertyChanged(nameof(IsLastPage));
        }
    }

    private void NextPage()
    {
        if (PageIndex < 3)
        {
            PageIndex++;
            OnPropertyChanged(nameof(IsFirstPage));
            OnPropertyChanged(nameof(IsLastPage));
        }
    }

    private async Task Close()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//history");
        }
    }
}
