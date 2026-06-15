namespace SC_LaborReporting.LaborReports
{
    public enum LaborReportStatus
    {
        Pending = 0,     // 审核中
        Withdrawn = 1,   // 撤回
        Rejected = 2,    // 退回
        Approved = 3     // 审批完成
    }
}