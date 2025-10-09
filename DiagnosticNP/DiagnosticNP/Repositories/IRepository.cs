using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiagnosticNP.Repositories
{
    public interface IRepository<T> where T : class, new()
    {
        Task<int> SaveAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> DeleteAsync(T entity);
        Task<T> GetByIdAsync(int id);
        Task<List<T>> GetAllAsync();
        Task<int> DeleteAllAsync();
    }
}