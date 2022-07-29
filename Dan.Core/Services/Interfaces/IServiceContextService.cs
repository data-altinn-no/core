using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

public interface IServiceContextService
{
    Task<List<ServiceContext>> GetRegisteredServiceContexts();
}