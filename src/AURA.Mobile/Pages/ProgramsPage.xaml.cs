using AURA.Mobile.ViewModels;
using Microsoft.Maui.Controls;

namespace AURA.Mobile.Pages;

public partial class ProgramsPage : ContentPage
{
    public ProgramsPage(ProgramsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
