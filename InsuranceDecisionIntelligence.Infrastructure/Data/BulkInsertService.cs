using InsuranceDecisionIntelligence.Application.Interfaces.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Data
{
    public class BulkInsertService : IBulkInsertService
    {
        public Task InsertAsync(DataTable dataTable)
        {
            throw new NotImplementedException();
        }
    }
}
