using KnowledgeMining.Domain.Entities.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Entities.Messages
{
    public record DocumentJobRequestMessage(DateTimeOffset expirationTime,
        string? Id,
        string? Index,
        string? Action
        );
}
