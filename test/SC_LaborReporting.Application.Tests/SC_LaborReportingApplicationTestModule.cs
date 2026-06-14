using Volo.Abp.Modularity;

namespace SC_LaborReporting;

[DependsOn(
    typeof(SC_LaborReportingApplicationModule),
    typeof(SC_LaborReportingDomainTestModule)
)]
public class SC_LaborReportingApplicationTestModule : AbpModule
{

}
