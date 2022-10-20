using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Domain.Enums
{
    public enum ServiceAction
    {
        Start,
        Refresh,
        Stop
    }

    public enum ServiceType
    {
        ExtractiveSummary,
        AbstractiveSummary
    }

    public enum ServiceState
    {
        Unknown,
        Pending,
        InProgress,
        Complete,
        Error
    }
}
