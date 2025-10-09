using DiagnosticNP.Models;
using DiagnosticNP.Services;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DiagnosticNP.Data
{
    public class EquipmentRepository : IEquipmentRepository
    {
        private readonly SQLiteAsyncConnection _database;

        public EquipmentRepository()
        {
            try
            {
                _database = new SQLiteAsyncConnection(DependencyService.Get<IDatabasePath>().GetDatabasePath());
                _database.CreateTableAsync<EquipmentNode>().Wait();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SaveEquipmentStructureAsync(List<EquipmentNode> nodes)
        {
            try
            {
                await _database.DeleteAllAsync<EquipmentNode>();

                // Сохраняем только плоский список узлов без Children
                var allNodes = GetAllNodesFlat(nodes);
                foreach (var node in allNodes)
                {
                    // Сохраняем только основные свойства, Children игнорируется
                    await _database.InsertAsync(node);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving equipment structure: {ex.Message}");
                return false;
            }
        }

        public async Task<List<EquipmentNode>> GetEquipmentStructureAsync()
        {
            try
            {
                var allNodes = await _database.Table<EquipmentNode>().ToListAsync();
                return BuildTreeStructure(allNodes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading equipment structure: {ex.Message}");
                return new List<EquipmentNode>();
            }
        }

        public async Task<bool> ClearEquipmentStructureAsync()
        {
            try
            {
                await _database.DeleteAllAsync<EquipmentNode>();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing equipment structure: {ex.Message}");
                return false;
            }
        }

        public async Task<EquipmentNode> FindNodeByPathAsync(string path)
        {
            try
            {
                return await _database.Table<EquipmentNode>()
                    .Where(n => n.FullPath == path)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding node by path: {ex.Message}");
                return null;
            }
        }

        private List<EquipmentNode> GetAllNodesFlat(List<EquipmentNode> nodes)
        {
            var result = new List<EquipmentNode>();

            foreach (var node in nodes)
            {
                // Создаем копию без Children для сохранения
                var nodeToSave = new EquipmentNode
                {
                    Id = node.Id,
                    Name = node.Name,
                    FullPath = node.FullPath,
                    ParentId = node.ParentId,
                    Type = node.Type
                };
                result.Add(nodeToSave);

                // Рекурсивно добавляем детей
                if (node.Children != null && node.Children.Any())
                {
                    result.AddRange(GetAllNodesFlat(node.Children));
                }
            }

            return result;
        }

        private List<EquipmentNode> BuildTreeStructure(List<EquipmentNode> allNodes)
        {
            var rootNodes = allNodes.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();

            foreach (var node in rootNodes)
            {
                BuildNodeChildren(node, allNodes);
            }

            return rootNodes;
        }

        private void BuildNodeChildren(EquipmentNode parent, List<EquipmentNode> allNodes)
        {
            var children = allNodes.Where(n => n.ParentId == parent.Id).ToList();
            parent.Children = children;

            foreach (var child in children)
            {
                BuildNodeChildren(child, allNodes);
            }
        }
    }
}