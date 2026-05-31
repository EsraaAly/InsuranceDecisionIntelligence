using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Application.Abstractions.Queue
{
    public interface IQueueService
    {
        ValueTask QueueBackgroundWorkItemAsync(string filePath);
        ValueTask<string> DequeueAsync(CancellationToken cancellationToken);
    }
}
