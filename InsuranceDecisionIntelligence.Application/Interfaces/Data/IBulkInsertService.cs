using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Interfaces.Data
{
    public interface IBulkInsertService
    {
        Task InsertAsync(string fileName, DataTable dataTable);
    }
}
