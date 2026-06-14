using SC_LaborReporting.Localization;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting;

/* Inherit your application services from this class.
 */
public abstract class SC_LaborReportingAppService : ApplicationService
{
    protected SC_LaborReportingAppService()
    {
        LocalizationResource = typeof(SC_LaborReportingResource);
    }
}
