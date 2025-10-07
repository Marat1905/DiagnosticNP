using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using DiagnosticNP.Services.Bluetooth;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiagnosticNP.Droid
{
    [Activity(Label = "DiagnosticNP", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // Явная инициализация NFC
            Plugin.NFC.CrossNFC.Init(this);

            // Запрашиваем разрешения
            RequestNecessaryPermissions();

            LoadApplication(new App());

            // Обработка NFC интента при запуске
            ProcessNFCIntent(Intent);
        }

        private void RequestNecessaryPermissions()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    var permissionsToRequest = new List<string>();

                    // Проверяем и добавляем необходимые разрешения
                    if (CheckSelfPermission(Manifest.Permission.AccessFineLocation) != Permission.Granted)
                        permissionsToRequest.Add(Manifest.Permission.AccessFineLocation);

                    if (CheckSelfPermission(Manifest.Permission.AccessCoarseLocation) != Permission.Granted)
                        permissionsToRequest.Add(Manifest.Permission.AccessCoarseLocation);

                    // Для Android 12+ нужны новые разрешения
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                    {
                        if (CheckSelfPermission(Manifest.Permission.BluetoothScan) != Permission.Granted)
                            permissionsToRequest.Add(Manifest.Permission.BluetoothScan);

                        if (CheckSelfPermission(Manifest.Permission.BluetoothConnect) != Permission.Granted)
                            permissionsToRequest.Add(Manifest.Permission.BluetoothConnect);

                        if (CheckSelfPermission(Manifest.Permission.BluetoothAdvertise) != Permission.Granted)
                            permissionsToRequest.Add(Manifest.Permission.BluetoothAdvertise);
                    }
                    else
                    {
                        // Для старых версий Android
                        if (CheckSelfPermission(Manifest.Permission.Bluetooth) != Permission.Granted)
                            permissionsToRequest.Add(Manifest.Permission.Bluetooth);

                        if (CheckSelfPermission(Manifest.Permission.BluetoothAdmin) != Permission.Granted)
                            permissionsToRequest.Add(Manifest.Permission.BluetoothAdmin);
                    }

                    if (permissionsToRequest.Any())
                    {
                        RequestPermissions(permissionsToRequest.ToArray(), 1001);
                    }
                    else
                    {
                        // Все разрешения уже есть, запускаем BLE сканер
                        BluetoothController.Restart();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Permission request error: {ex.Message}");
            }
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
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            try
            {
                Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

                if (requestCode == 1001)
                {
                    bool allGranted = grantResults.All(result => result == Permission.Granted);

                    if (allGranted)
                    {
                        System.Diagnostics.Debug.WriteLine("All permissions granted, starting BLE scanner");
                        BluetoothController.Restart();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Some permissions were denied");
                        // Можно показать сообщение пользователю
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in permission result: {ex.Message}");
            }
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