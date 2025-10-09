using DiagnosticNP.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiagnosticNP.Repositories
{
    public class ControlPointRepository : IRepository<ControlPoint>
    {
        private readonly SQLiteAsyncConnection _database;

        public ControlPointRepository(SQLiteAsyncConnection database)
        {
            _database = database;
            // Создаем таблицу, если не существует
            _database.CreateTableAsync<ControlPoint>().Wait();
        }

        public async Task<int> SaveAsync(ControlPoint entity)
        {
            try
            {
                // Проверяем, существует ли уже запись с таким ID
                var existing = await _database.FindAsync<ControlPoint>(entity.Id);
                if (existing != null)
                {
                    return await _database.UpdateAsync(entity);
                }
                else
                {
                    return await _database.InsertAsync(entity);
                }
            }
            catch (SQLiteException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка SQLite при сохранении: {ex.Message}");
                return 0;
            }
        }

        public Task<int> UpdateAsync(ControlPoint entity)
        {
            return _database.UpdateAsync(entity);
        }

        public Task<int> DeleteAsync(ControlPoint entity)
        {
            return _database.DeleteAsync(entity);
        }

        public Task<ControlPoint> GetByIdAsync(int id)
        {
            return _database.Table<ControlPoint>().Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        public Task<List<ControlPoint>> GetAllAsync()
        {
            return _database.Table<ControlPoint>().ToListAsync();
        }

        public Task<int> DeleteAllAsync()
        {
            return _database.DeleteAllAsync<ControlPoint>();
        }

        public async Task<List<ControlPoint>> GetByParentIdAsync(int? parentId)
        {
            return await _database.Table<ControlPoint>()
                .Where(x => x.ParentId == parentId)
                .ToListAsync();
        }

        public async Task<ControlPoint> GetByPathAsync(string fullPath)
        {
            return await _database.Table<ControlPoint>()
                .Where(x => x.FullPath == fullPath)
                .FirstOrDefaultAsync();
        }

        // Метод для вставки или замены (более надежный)
        public async Task<int> InsertOrReplaceAsync(ControlPoint entity)
        {
            return await _database.InsertOrReplaceAsync(entity);
        }

        // Новый метод для построения полного дерева
        public async Task<List<ControlPoint>> GetTreeAsync()
        {
            try
            {
                var allPoints = await GetAllAsync();
                System.Diagnostics.Debug.WriteLine($"Всего точек в БД: {allPoints?.Count ?? 0}");

                if (allPoints == null || !allPoints.Any())
                {
                    System.Diagnostics.Debug.WriteLine("БД пуста");
                    return new List<ControlPoint>();
                }

                var tree = BuildTree(allPoints);
                System.Diagnostics.Debug.WriteLine($"Построено дерево с {tree.Count} корневыми элементами");
                return tree;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при построении дерева: {ex.Message}");
                return new List<ControlPoint>();
            }
        }

        private List<ControlPoint> BuildTree(List<ControlPoint> allPoints)
        {
            var rootPoints = allPoints.Where(p => p.ParentId == null).ToList();
            System.Diagnostics.Debug.WriteLine($"Корневых точек: {rootPoints.Count}");

            foreach (var root in rootPoints)
            {
                BuildTreeRecursive(root, allPoints);
            }

            return rootPoints;
        }

        private void BuildTreeRecursive(ControlPoint parent, List<ControlPoint> allPoints)
        {
            var children = allPoints.Where(p => p.ParentId == parent.Id).ToList();
            System.Diagnostics.Debug.WriteLine($"Для {parent.Name} найдено детей: {children.Count}");

            foreach (var child in children)
            {
                parent.AddChild(child);
                BuildTreeRecursive(child, allPoints);
            }
        }
    }
}