using SC_LaborReporting.Books;
using Xunit;

namespace SC_LaborReporting.EntityFrameworkCore.Applications.Books;

[Collection(SC_LaborReportingTestConsts.CollectionDefinitionName)]
public class EfCoreBookAppService_Tests : BookAppService_Tests<SC_LaborReportingEntityFrameworkCoreTestModule>
{

}