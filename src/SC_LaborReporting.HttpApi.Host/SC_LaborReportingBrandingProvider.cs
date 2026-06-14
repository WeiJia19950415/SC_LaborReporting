using Microsoft.Extensions.Localization;
using SC_LaborReporting.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace SC_LaborReporting;

[Dependency(ReplaceServices = true)]
public class SC_LaborReportingBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<SC_LaborReportingResource> _localizer;

    public SC_LaborReportingBrandingProvider(IStringLocalizer<SC_LaborReportingResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
