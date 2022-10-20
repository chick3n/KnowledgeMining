using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public record QueueReceipt(string? messageId, 
        DateTimeOffset insertionTime = default(DateTimeOffset), 
        DateTimeOffset expirationTime = default(DateTimeOffset), 
        string? popReceipt = null, 
        DateTimeOffset timeNextVisible = default(DateTimeOffset));
}
