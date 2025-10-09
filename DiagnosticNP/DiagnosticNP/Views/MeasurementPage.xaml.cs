using DiagnosticNP.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DiagnosticNP.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MeasurementPage : ContentPage
    {
        public MeasurementPage(Models.ControlPoint controlPoint, MainViewModel mainViewModel)
        {
            InitializeComponent();
            BindingContext = new MeasurementViewModel(controlPoint, mainViewModel);
        }
    }
}