using DiagnosticNP.Views;
using Xamarin.Forms;

namespace DiagnosticNP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Njc4NTkxQDMyMzAyZTMyMmUzMFdGQkpoWWhnZEsrNUc1amo1b1R1eXF6TXVlNm8vRzA4RHQzZGI5Umd6ZlU9");

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
