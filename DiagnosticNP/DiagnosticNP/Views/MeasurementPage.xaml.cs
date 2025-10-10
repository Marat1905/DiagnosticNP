using DiagnosticNP.ViewModels;
using Xamarin.Forms;

namespace DiagnosticNP.Views
{
    public partial class MeasurementPage : ContentPage
    {
        public MeasurementPage()
        {
            InitializeComponent();
        }

        public MeasurementPage(MeasurementViewModel viewModel) : this()
        {
            BindingContext = viewModel;
        }

        private async void OnCancelClicked(object sender, System.EventArgs e)
        {
            if (BindingContext is MeasurementViewModel vm)
            {
                vm.Dispose();
            }
            await Navigation.PopAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is MeasurementViewModel vm)
            {
                vm.Dispose();
            }
        }
    }
}