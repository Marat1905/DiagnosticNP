using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Content;

namespace DiagnosticNP.Droid
{
    [Activity(Label = "DiagnosticNP", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            

            // Явная инициализация NFC
            Plugin.NFC.CrossNFC.Init(this);

            LoadApplication(new App());

            // Обработка NFC интента при запуске
            ProcessNFCIntent(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            // Обрабатываем новый интент NFC
            ProcessNFCIntent(intent);
            Plugin.NFC.CrossNFC.OnNewIntent(intent);
        }

        protected override void OnResume()
        {
            base.OnResume();
            Plugin.NFC.CrossNFC.OnResume();
        }

        protected override void OnPause()
        {
            base.OnPause();
            //Plugin.NFC.CrossNFC.OnPause();
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void ProcessNFCIntent(Intent intent)
        {
            if (intent?.Action == null)
                return;

            var nfcActions = new[]
            {
                "android.nfc.action.NDEF_DISCOVERED",
                "android.nfc.action.TAG_DISCOVERED",
                "android.nfc.action.TECH_DISCOVERED"
            };

            foreach (var action in nfcActions)
            {
                if (intent.Action.Equals(action))
                {
                    Plugin.NFC.CrossNFC.OnNewIntent(intent);
                    break;
                }
            }
        }
    }
}