using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiagnosticNP.Models.Equipment;
using SQLite;
using Xamarin.Forms;

namespace DiagnosticNP.Services.Database
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly SQLiteConnection _database;

        public EquipmentRepository()
        {
            _database = DependencyService.Get<IDatabaseService>().GetConnection();
            InitializeTables();
        }

        private void InitializeTables()
        {
            _database.CreateTable<EquipmentNode>();
            _database.CreateTable<MeasurementData>();
        }

        public Task<List<EquipmentNode>> GetAllNodesAsync()
        {
            return Task.FromResult(_database.Table<EquipmentNode>().ToList());
        }

        public Task<EquipmentNode> GetNodeByIdAsync(string id)
        {
            return Task.FromResult(_database.Table<EquipmentNode>()
                .FirstOrDefault(n => n.Id == id));
        }

        public Task<List<EquipmentNode>> GetNodesByParentIdAsync(string parentId)
        {
            return Task.FromResult(_database.Table<EquipmentNode>()
                .Where(n => n.ParentId == parentId)
                .ToList());
        }

        public Task SaveNodeAsync(EquipmentNode node)
        {
            _database.InsertOrReplace(node);
            return Task.CompletedTask;
        }

        public Task SaveNodesAsync(List<EquipmentNode> nodes)
        {
            _database.RunInTransaction(() =>
            {
                foreach (var node in nodes)
                {
                    _database.InsertOrReplace(node);
                }
            });
            return Task.CompletedTask;
        }

        public Task DeleteAllNodesAsync()
        {
            _database.DeleteAll<EquipmentNode>();
            return Task.CompletedTask;
        }

        public Task<List<EquipmentNode>> GetNodesByNfcFilterAsync(string nfcFilter)
        {
            if (string.IsNullOrEmpty(nfcFilter))
                return Task.FromResult(new List<EquipmentNode>());

            return Task.FromResult(_database.Table<EquipmentNode>()
                .Where(n => n.NfcFilter != null &&
                           n.NfcFilter.ToLower().Contains(nfcFilter.ToLower()))
                .ToList());
        }

        public Task SaveMeasurementAsync(MeasurementData measurement)
        {
            _database.InsertOrReplace(measurement);
            return Task.CompletedTask;
        }

        public Task<List<MeasurementData>> GetMeasurementsByNodeIdAsync(string nodeId)
        {
            return Task.FromResult(_database.Table<MeasurementData>()
                .Where(m => m.NodeId == nodeId)
                .ToList());
        }

        public Task<List<MeasurementData>> GetUnsyncedMeasurementsAsync()
        {
            return Task.FromResult(_database.Table<MeasurementData>()
                .Where(m => !m.IsSynced)
                .ToList());
        }

        public Task MarkMeasurementsAsSyncedAsync()
        {
            var unsynced = _database.Table<MeasurementData>().Where(m => !m.IsSynced).ToList();
            foreach (var measurement in unsynced)
            {
                measurement.IsSynced = true;
                _database.Update(measurement);
            }
            return Task.CompletedTask;
        }

        public Task DeleteAllMeasurementsAsync()
        {
            _database.DeleteAll<MeasurementData>();
            return Task.CompletedTask;
        }
    }
}