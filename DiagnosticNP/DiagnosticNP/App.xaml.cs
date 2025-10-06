using DiagnosticNP.Services;
using DiagnosticNP.Views;
using Xamarin.Forms;

namespace DiagnosticNP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Инициализация базы данных
            DatabaseService.Init();

            MainPage = new NavigationPage(new MainPage());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
