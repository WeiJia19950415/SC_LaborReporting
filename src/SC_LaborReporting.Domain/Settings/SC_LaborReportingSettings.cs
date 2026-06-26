namespace SC_LaborReporting.Settings;

public static class SC_LaborReportingSettings
{
    private const string Prefix = "SC_LaborReporting";

    //Add your own setting names here. Example:
    //public const string MySetting1 = Prefix + ".MySetting1";

    // 考勤期间起始日期
    public const string AttendanceStartDate = Prefix + ".AttendanceStartDate";
    // 考勤期间截至日期
    public const string AttendanceEndDate = Prefix + ".AttendanceEndDate";
    // 审计状态
    public const string AuditStatus = Prefix + ".AuditStatus";
}
