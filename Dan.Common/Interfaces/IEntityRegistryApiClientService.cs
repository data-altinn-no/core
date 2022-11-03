using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Common.Interfaces;
public interface IEntityRegistryApiClientService
{
    public Task<EntityRegistryUnit?> GetUpstreamEntityRegistryUnitAsync(Uri registryApiUri);
}
