using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceMatic.Test
{
    [RegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient)]
    public class AttributeService: IAttributeService
    {
    }

    public interface IAttributeService
    {
    }
}
