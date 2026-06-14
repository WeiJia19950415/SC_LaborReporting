using SC_LaborReporting.Samples;
using Xunit;

namespace SC_LaborReporting.EntityFrameworkCore.Domains;

[Collection(SC_LaborReportingTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<SC_LaborReportingEntityFrameworkCoreTestModule>
{

}
