using Firebasemauiapp.Mainpages;
using Firebasemauiapp.Services;
using Microsoft.Maui.Graphics;

namespace Firebasemauiapp.Mainpages;

public partial class SummaryMockView : ContentPage
{
    public SummaryMockView()
    {
    }

    public SummaryMockView(SummaryViewModel vm)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);

        // Use injected SummaryViewModel and seed mock data
        BindingContext = vm;

        // Mock data resembling production shape
        vm.SetData(
            content: "After her performance, I kept replaying that moment in my head. It wasn’t dramatic or life-changing or anything, but it genuinely made my day feel softer. I walked out of the event feeling lighter, almost like floating. It’s strange how a single person, someone I’ve never even met, can make me feel this way just by smiling. But she did. And I’m really grateful for that small, beautiful moment.",
            mood: "joy",
            suggestion: "Write a thank-you note to express your appreciation.",
            keywords: "Mabel, concert, crowd, Pixxie, tired",
            emotion: "positive",
            score: "8");

        // Safety net: if for any reason no cards were generated, inject a visible demo set

    }




}
