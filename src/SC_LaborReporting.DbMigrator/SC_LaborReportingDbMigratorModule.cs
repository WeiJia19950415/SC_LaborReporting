using SC_LaborReporting.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace SC_LaborReporting.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(SC_LaborReportingEntityFrameworkCoreModule),
    typeof(SC_LaborReportingApplicationContractsModule)
)]
public class SC_LaborReportingDbMigratorModule : AbpModule
{
}
