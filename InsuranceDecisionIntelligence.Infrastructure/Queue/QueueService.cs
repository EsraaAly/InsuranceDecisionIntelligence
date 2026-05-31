using InsuranceDecisionIntelligence.Application.Abstractions.Queue;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace InsuranceDecisionIntelligence.Infrastructure.Queue
{
    public class QueueService:IQueueService
    {
        private readonly Channel<string> _queue;

        public QueueService()
        {
            var options = new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait };
            _queue = Channel.CreateBounded<string>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(string filePath) =>
            await _queue.Writer.WriteAsync(filePath);

        public async ValueTask<string> DequeueAsync(CancellationToken cancellationToken) =>
            await _queue.Reader.ReadAsync(cancellationToken);
    }
}
