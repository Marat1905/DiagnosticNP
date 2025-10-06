using DiagnosticNP.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DiagnosticNP.Services
{
    public interface IDatabaseService
    {
        Task SaveControlPointsAsync(List<ControlPoint> controlPoints);
        Task<List<ControlPoint>> GetControlPointsAsync();
        Task SaveMeasurementAsync(MeasurementData measurement);
        Task<List<MeasurementData>> GetAllMeasurementsAsync();
        Task ClearMeasurementsAsync();
        Task ClearControlPointsAsync();
    }

    public class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            Init();
        }

        public static void Init()
        {
            var databasePath = GetDatabasePath();
            var database = new SQLiteAsyncConnection(databasePath);

            database.CreateTableAsync<ControlPoint>().Wait();
            database.CreateTableAsync<MeasurementData>().Wait();
        }

        private SQLiteAsyncConnection Database
        {
            get
            {
                if (_database == null)
                {
                    var databasePath = GetDatabasePath();
                    _database = new SQLiteAsyncConnection(databasePath);
                }
                return _database;
            }
        }

        private static string GetDatabasePath()
        {
            var databaseName = "DiagnosticNP.db3";
            return DependencyService.Get<IDatabasePath>().GetDatabasePath(databaseName);
        }

        public async Task SaveControlPointsAsync(List<ControlPoint> controlPoints)
        {
            try
            {
                // Удаляем старые данные
                await ClearControlPointsAsync();

                // Сохраняем новые данные
                foreach (var point in controlPoints)
                {
                    await Database.InsertAsync(point);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save ControlPoints Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ControlPoint>> GetControlPointsAsync()
        {
            try
            {
                return await Database.Table<ControlPoint>().ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get ControlPoints Error: {ex.Message}");
                return new List<ControlPoint>();
            }
        }

        public async Task SaveMeasurementAsync(MeasurementData measurement)
        {
            try
            {
                await Database.InsertAsync(measurement);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save Measurement Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<MeasurementData>> GetAllMeasurementsAsync()
        {
            try
            {
                return await Database.Table<MeasurementData>().ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get Measurements Error: {ex.Message}");
                return new List<MeasurementData>();
            }
        }

        public async Task ClearMeasurementsAsync()
        {
            try
            {
                await Database.DeleteAllAsync<MeasurementData>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear Measurements Error: {ex.Message}");
            }
        }

        public async Task ClearControlPointsAsync()
        {
            try
            {
                await Database.DeleteAllAsync<ControlPoint>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear ControlPoints Error: {ex.Message}");
            }
        }
    }
}