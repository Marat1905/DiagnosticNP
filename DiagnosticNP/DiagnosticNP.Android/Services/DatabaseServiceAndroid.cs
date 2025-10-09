using Android.App;
using Android.Content;
using DiagnosticNP.Services;
using SQLite;
using System;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(DiagnosticNP.Droid.Services.DatabaseServiceAndroid))]
namespace DiagnosticNP.Droid.Services
{
    public class DatabaseServiceAndroid : IDatabaseService
    {
        public SQLiteAsyncConnection GetConnection()
        {
            var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "diagnostic.db");
            return new SQLiteAsyncConnection(databasePath);
        }
    }
}