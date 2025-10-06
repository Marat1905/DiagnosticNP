using DiagnosticNP.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DiagnosticNP.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ControlPointsPage : ContentPage
    {
        public ControlPointsPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ControlPointsViewModel vm)
            {
                vm.LoadData();
            }
        }
    }
}