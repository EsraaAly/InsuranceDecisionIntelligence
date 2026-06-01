using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Contracts.Event_Driven
{
    public record FileUploadedEvent(string FilePath, string FileName);
}
