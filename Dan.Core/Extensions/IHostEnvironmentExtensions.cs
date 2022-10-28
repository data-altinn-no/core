using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Dan.Core.Extensions;
public static class HostEnvironmentExtensions
{
    public static bool IsLocalDevelopment(this IHostEnvironment _)
    {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "LocalDevelopment";
    }
}
