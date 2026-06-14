using Xunit;

namespace SC_LaborReporting.EntityFrameworkCore;

[CollectionDefinition(SC_LaborReportingTestConsts.CollectionDefinitionName)]
public class SC_LaborReportingEntityFrameworkCoreCollection : ICollectionFixture<SC_LaborReportingEntityFrameworkCoreFixture>
{

}
