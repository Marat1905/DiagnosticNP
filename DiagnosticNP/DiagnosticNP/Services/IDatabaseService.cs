using SQLite;
using System.Threading.Tasks;

namespace DiagnosticNP.Services
{
    public interface IDatabaseService
    {
        SQLiteAsyncConnection GetConnection();
    }
}