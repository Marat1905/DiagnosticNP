using DiagnosticNP.Models;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiagnosticNP.Repositories
{
    public class MeasurementRepository : IRepository<Measurement>
    {
        private readonly SQLiteAsyncConnection _database;

        public MeasurementRepository(SQLiteAsyncConnection database)
        {
            _database = database;
            _database.CreateTableAsync<Measurement>().Wait();
        }

        public Task<int> SaveAsync(Measurement entity)
        {
            return _database.InsertAsync(entity);
        }

        public Task<int> UpdateAsync(Measurement entity)
        {
            return _database.UpdateAsync(entity);
        }

        public Task<int> DeleteAsync(Measurement entity)
        {
            return _database.DeleteAsync(entity);
        }

        public Task<Measurement> GetByIdAsync(int id)
        {
            return _database.Table<Measurement>().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public Task<List<Measurement>> GetAllAsync()
        {
            return _database.Table<Measurement>().ToListAsync();
        }

        public Task<int> DeleteAllAsync()
        {
            return _database.DeleteAllAsync<Measurement>();
        }

        public async Task<List<Measurement>> GetByControlPointAsync(int controlPointId)
        {
            return await _database.Table<Measurement>()
                .Where(x => x.ControlPointId == controlPointId)
                .ToListAsync();
        }

        public async Task<List<Measurement>> GetPendingUploadsAsync()
        {
            return await _database.Table<Measurement>().ToListAsync();
        }
    }
}