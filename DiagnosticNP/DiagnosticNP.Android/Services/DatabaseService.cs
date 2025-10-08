using DiagnosticNP.Droid.Services;
using DiagnosticNP.Services.Database;
using SQLite;
using System;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(DatabaseService))]
namespace DiagnosticNP.Droid.Services
{
    public class DatabaseService : IDatabaseService
    {
        public SQLiteConnection GetConnection()
        {
            var databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "diagnosticnp.db3");
            return new SQLiteConnection(databasePath);
        }

        public async Task InitializeDatabaseAsync()
        {
            // База данных будет создана автоматически при первом подключении
            await Task.CompletedTask;
        }
    }
}