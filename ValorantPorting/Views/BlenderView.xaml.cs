using ValorantPorting.ViewModels;

namespace ValorantPorting.Views;

public partial class BlenderView
{
    public BlenderView()
    {
        InitializeComponent();
        AppVM.BlenderVM = new BlenderViewModel();
        DataContext = AppVM.BlenderVM;
    }
}