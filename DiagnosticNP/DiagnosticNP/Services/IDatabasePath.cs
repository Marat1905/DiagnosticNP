using System;
using System.Collections.Generic;
using System.Text;

namespace DiagnosticNP.Services
{
    public interface IDatabasePath
    {
        string GetDatabasePath(string databaseName);
    }
}
