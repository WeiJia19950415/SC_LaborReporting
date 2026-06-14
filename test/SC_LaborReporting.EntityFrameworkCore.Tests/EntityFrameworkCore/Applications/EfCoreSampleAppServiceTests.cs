using SC_LaborReporting.Samples;
using Xunit;

namespace SC_LaborReporting.EntityFrameworkCore.Applications;

[Collection(SC_LaborReportingTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<SC_LaborReportingEntityFrameworkCoreTestModule>
{

}
