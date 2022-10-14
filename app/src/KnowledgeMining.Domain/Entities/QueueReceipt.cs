using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities
{
    public record QueueReceipt(string messageId, 
        DateTimeOffset insertionTime, 
        DateTimeOffset expirationTime, 
        string popReceipt, 
        DateTimeOffset timeNextVisible);
}
