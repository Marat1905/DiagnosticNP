using System;
using System.IO;
using DiagnosticNP.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(DiagnosticNP.Droid.Services.DatabasePath))]
namespace DiagnosticNP.Droid.Services
{
    public class DatabasePath : IDatabasePath
    {
        public string GetDatabasePath(string databaseName)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return Path.Combine(path, databaseName);
        }
    }
}