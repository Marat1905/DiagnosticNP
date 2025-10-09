using DiagnosticNP.Views;
using Syncfusion.Licensing;
using Xamarin.Forms;

namespace DiagnosticNP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Регистрация лицензии Syncfusion
            SyncfusionLicenseProvider.RegisterLicense("Njc4NTkxQDMyMzAyZTMyMmUzMFdGQkpoWWhnZEsrNUc1amo1b1R1eXF6TXVlNm8vRzA4RHQzZGI5Umd6ZlU9");

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
