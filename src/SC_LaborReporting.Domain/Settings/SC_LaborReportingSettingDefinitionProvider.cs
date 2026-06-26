using Volo.Abp.Settings;

namespace SC_LaborReporting.Settings;

public class SC_LaborReportingSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(SC_LaborReportingSettings.AttendanceStartDate, "26", isVisibleToClients: true),
            new SettingDefinition(SC_LaborReportingSettings.AttendanceEndDate, "25", isVisibleToClients: true),
            new SettingDefinition(SC_LaborReportingSettings.AuditStatus, "false", isVisibleToClients: true)
        );
    }
}
