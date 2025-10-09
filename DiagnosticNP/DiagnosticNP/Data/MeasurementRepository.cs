using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiagnosticNP.Models;
using DiagnosticNP.Services;
using SQLite;
using Xamarin.Forms;

namespace DiagnosticNP.Data
{
    public class MeasurementRepository : IMeasurementRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public MeasurementRepository()
        {
            _database = new SQLiteAsyncConnection(DependencyService.Get<IDatabasePath>().GetDatabasePath());
            _database.CreateTableAsync<MeasurementData>().Wait();
        }

        public async Task<int> SaveMeasurementAsync(MeasurementData measurement)
        {
            try
            {
                if (measurement.Id == 0)
                {
                    return await _database.InsertAsync(measurement);
                }
                else
                {
                    await _database.UpdateAsync(measurement);
                    return measurement.Id;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving measurement: {ex.Message}");
                return -1;
            }
        }

        public async Task<List<MeasurementData>> GetMeasurementsByNodeAsync(string nodeId)
        {
            try
            {
                return await _database.Table<MeasurementData>()
                    .Where(m => m.NodeId == nodeId)
                    .OrderByDescending(m => m.MeasurementTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting measurements by node: {ex.Message}");
                return new List<MeasurementData>();
            }
        }

        public async Task<List<MeasurementData>> GetUnsyncedMeasurementsAsync()
        {
            try
            {
                return await _database.Table<MeasurementData>()
                    .Where(m => !m.IsSynced)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unsynced measurements: {ex.Message}");
                return new List<MeasurementData>();
            }
        }

        public async Task<bool> MarkAsSyncedAsync(int measurementId)
        {
            try
            {
                var measurement = await _database.Table<MeasurementData>()
                    .FirstOrDefaultAsync(m => m.Id == measurementId);

                if (measurement != null)
                {
                    measurement.IsSynced = true;
                    await _database.UpdateAsync(measurement);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking measurement as synced: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClearMeasurementsAsync()
        {
            try
            {
                await _database.DeleteAllAsync<MeasurementData>();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing measurements: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteMeasurementAsync(int measurementId)
        {
            try
            {
                var measurement = await _database.Table<MeasurementData>()
                    .FirstOrDefaultAsync(m => m.Id == measurementId);

                if (measurement != null)
                {
                    await _database.DeleteAsync(measurement);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting measurement: {ex.Message}");
                return false;
            }
        }

        public async Task<MeasurementData> GetLatestMeasurementAsync(string nodeId, string direction)
        {
            try
            {
                return await _database.Table<MeasurementData>()
                    .Where(m => m.NodeId == nodeId && m.Direction == direction)
                    .OrderByDescending(m => m.MeasurementTime)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting latest measurement: {ex.Message}");
                return null;
            }
        }
    }
}