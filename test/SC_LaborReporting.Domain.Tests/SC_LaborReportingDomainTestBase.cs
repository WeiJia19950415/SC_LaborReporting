using Volo.Abp.Modularity;

namespace SC_LaborReporting;

/* Inherit from this class for your domain layer tests. */
public abstract class SC_LaborReportingDomainTestBase<TStartupModule> : SC_LaborReportingTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
