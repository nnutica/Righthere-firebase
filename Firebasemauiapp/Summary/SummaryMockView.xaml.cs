using Firebasemauiapp.Mainpages;
using Firebasemauiapp.Services;
using Microsoft.Maui.Graphics;

namespace Firebasemauiapp.Mainpages;

public partial class SummaryMockView : ContentPage
{
    public SummaryMockView()
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);

        // Resolve the real SummaryViewModel via DI and seed mock data
        var vm = ServiceHelper.Get<SummaryViewModel>();
        BindingContext = vm;

        // Mock data resembling production shape
        vm.SetData(
            content: "Had dinner with family, received a small gift, feeling grateful.",
            mood: "joy",
            suggestion: "Write a thank-you note to express your appreciation.",
            keywords: "Mabel, concert, crowd, Pixxie, tired",
            emotion: "positive",
            score: "8.5");

        // Safety net: if for any reason no cards were generated, inject a visible demo set
        
    }

     

    
}
