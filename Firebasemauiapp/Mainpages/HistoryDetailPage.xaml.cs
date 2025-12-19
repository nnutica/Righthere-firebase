using Firebasemauiapp.Model;

namespace Firebasemauiapp.Mainpages;

public partial class HistoryDetailPage : ContentPage, IQueryAttributable
{
    private readonly HistoryDetailViewModel _viewModel;

    public HistoryDetailPage(HistoryDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Diary", out var diaryObj) && diaryObj is DiaryData diary)
        {
            _viewModel.SetData(diary);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine("HistoryDetailPage appearing");
    }
}
