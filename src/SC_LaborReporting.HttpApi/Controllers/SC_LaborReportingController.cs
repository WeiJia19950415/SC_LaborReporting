using SC_LaborReporting.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace SC_LaborReporting.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class SC_LaborReportingController : AbpControllerBase
{
    protected SC_LaborReportingController()
    {
        LocalizationResource = typeof(SC_LaborReportingResource);
    }
}
