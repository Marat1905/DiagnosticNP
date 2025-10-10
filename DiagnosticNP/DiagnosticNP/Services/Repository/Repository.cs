using DiagnosticNP.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DiagnosticNP.Services.Repository
{
    public class Repository : IRepository
    {
        private SQLiteAsyncConnection _database;

        public Repository()
        {
            InitializeAsync().ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            try
            {
                var databasePath = DependencyService.Get<IDatabasePath>().GetDatabasePath();
                _database = new SQLiteAsyncConnection(databasePath);

                await InitializeDatabaseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Repository initialization error: {ex.Message}");
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                await _database.CreateTableAsync<EquipmentNode>();
                await _database.CreateTableAsync<Measurement>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database creation error: {ex.Message}");
            }
        }

        public async Task SaveEquipmentNodesAsync(List<EquipmentNode> nodes)
        {
            try
            {
                await ClearEquipmentNodesAsync();
                await _database.InsertAllAsync(nodes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save equipment nodes error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EquipmentNode>> GetEquipmentNodesAsync()
        {
            try
            {
                return await _database.Table<EquipmentNode>().ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get equipment nodes error: {ex.Message}");
                return new List<EquipmentNode>();
            }
        }

        public async Task ClearEquipmentNodesAsync()
        {
            try
            {
                await _database.DeleteAllAsync<EquipmentNode>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear equipment nodes error: {ex.Message}");
            }
        }

        public async Task SaveMeasurementAsync(Measurement measurement)
        {
            try
            {
                await _database.InsertAsync(measurement);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save measurement error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Measurement>> GetMeasurementsAsync(bool includeUploaded = false)
        {
            try
            {
                if (includeUploaded)
                    return await _database.Table<Measurement>().ToListAsync();
                else
                    return await _database.Table<Measurement>().Where(m => !m.IsUploaded).ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get measurements error: {ex.Message}");
                return new List<Measurement>();
            }
        }

        public async Task<List<Measurement>> GetPendingUploadMeasurementsAsync()
        {
            try
            {
                return await _database.Table<Measurement>().Where(m => !m.IsUploaded).ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get pending measurements error: {ex.Message}");
                return new List<Measurement>();
            }
        }

        public async Task MarkAsUploadedAsync(int measurementId)
        {
            try
            {
                var measurement = await _database.Table<Measurement>().Where(m => m.Id == measurementId).FirstOrDefaultAsync();
                if (measurement != null)
                {
                    measurement.IsUploaded = true;
                    await _database.UpdateAsync(measurement);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mark as uploaded error: {ex.Message}");
            }
        }

        public async Task DeleteUploadedMeasurementsAsync()
        {
            try
            {
                await _database.Table<Measurement>().Where(m => m.IsUploaded).DeleteAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete uploaded measurements error: {ex.Message}");
            }
        }

        public async Task ClearAllMeasurementsAsync()
        {
            try
            {
                await _database.DeleteAllAsync<Measurement>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear all measurements error: {ex.Message}");
            }
        }
    }
}