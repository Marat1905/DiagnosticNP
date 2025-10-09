using System;
using DiagnosticNP.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(DiagnosticNP.Droid.DatabasePath))]
namespace DiagnosticNP.Droid
{
    public class DatabasePath : IDatabasePath
    {
        public string GetDatabasePath()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return System.IO.Path.Combine(path, "diagnostic.db3");
        }
    }
}