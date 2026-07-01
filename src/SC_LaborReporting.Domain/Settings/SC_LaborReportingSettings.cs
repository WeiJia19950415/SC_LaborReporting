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

    /// <summary>
    /// 企业微信CorpID设置
    /// </summary>
    public const string WeComCorpID = Prefix + ".WeComCorpID";

    /// <summary>
    /// 企业微信AgentId设置
    /// </summary>
    public const string WeComAgentId = Prefix + ".WeComAgentId";


    /// <summary>
    /// 企业微信Secret设置
    /// </summary>
    public const string WeComSecret = Prefix + ".WeComSecret";
}
