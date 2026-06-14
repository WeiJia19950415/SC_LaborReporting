using Volo.Abp.Modularity;

namespace SC_LaborReporting;

[DependsOn(
    typeof(SC_LaborReportingDomainModule),
    typeof(SC_LaborReportingTestBaseModule)
)]
public class SC_LaborReportingDomainTestModule : AbpModule
{

}
