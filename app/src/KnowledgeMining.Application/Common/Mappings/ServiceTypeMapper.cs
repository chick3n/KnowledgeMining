using KnowledgeMining.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeMining.Application.Common.Mappings
{
    public class ServiceTypeMapper
    {
        public static ServiceType FromString(string serviceTypeValue)
        {
            if (string.IsNullOrEmpty(serviceTypeValue))
                throw new ArgumentNullException(nameof(serviceTypeValue));

            if(serviceTypeValue.Equals(ServiceType.AbstractiveSummary.ToString(), StringComparison.OrdinalIgnoreCase))
                return ServiceType.AbstractiveSummary;

            if (serviceTypeValue.Equals(ServiceType.Prompt.ToString(), StringComparison.OrdinalIgnoreCase))
                return ServiceType.Prompt;

            if (serviceTypeValue.Equals(ServiceType.ExtractiveSummary.ToString(), StringComparison.OrdinalIgnoreCase))
                return ServiceType.ExtractiveSummary;

            throw new ArgumentNullException($"{nameof(serviceTypeValue)} {serviceTypeValue} does not exist.");
        }
    }
}
