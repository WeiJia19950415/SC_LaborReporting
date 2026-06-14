using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SC_LaborReporting.Data;
using Volo.Abp.DependencyInjection;

namespace SC_LaborReporting.EntityFrameworkCore;

public class EntityFrameworkCoreSC_LaborReportingDbSchemaMigrator
    : ISC_LaborReportingDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreSC_LaborReportingDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the SC_LaborReportingDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<SC_LaborReportingDbContext>()
            .Database
            .MigrateAsync();
    }
}
