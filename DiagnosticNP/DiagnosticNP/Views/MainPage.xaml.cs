using Xamarin.Forms;

namespace DiagnosticNP.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            //base.OnDisappearing();
            //if (BindingContext is ViewModels.MainViewModel vm)
            //{
            //    vm.OnDisappearing();
            //}
        }
    }
}