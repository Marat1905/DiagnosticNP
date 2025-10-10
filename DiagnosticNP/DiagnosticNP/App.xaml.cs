using DiagnosticNP.ViewModels;
using DiagnosticNP.Views;
using System.Globalization;
using System.Threading;
using Xamarin.Forms;

namespace DiagnosticNP
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Njc4NTkxQDMyMzAyZTMyMmUzMFdGQkpoWWhnZEsrNUc1amo1b1R1eXF6TXVlNm8vRzA4RHQzZGI5Umd6ZlU9");
            // Установите инвариантную культуру для всего приложения
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            var mainPage = new MainPage();
            mainPage.BindingContext = new MainViewModel();
            MainPage = new NavigationPage(mainPage);
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
