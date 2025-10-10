using System;
using System.IO;
using DiagnosticNP.Services;
using Xamarin.Forms;

[assembly: Dependency(typeof(DiagnosticNP.Droid.DatabasePathAndroid))]
namespace DiagnosticNP.Droid
{
    public class DatabasePathAndroid : IDatabasePath
    {
        public string GetDatabasePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "diagnostic.db");
        }
    }
}