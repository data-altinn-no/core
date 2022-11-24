using Microsoft.Azure.Functions.Worker;

namespace Dan.Core.Services.Interfaces;
public interface IFunctionContextAccessor
{
    FunctionContext FunctionContext { get; set; }
}
