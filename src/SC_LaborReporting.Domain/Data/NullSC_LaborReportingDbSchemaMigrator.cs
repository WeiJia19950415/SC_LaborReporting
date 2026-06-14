using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace SC_LaborReporting.Data;

/* This is used if database provider does't define
 * ISC_LaborReportingDbSchemaMigrator implementation.
 */
public class NullSC_LaborReportingDbSchemaMigrator : ISC_LaborReportingDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
