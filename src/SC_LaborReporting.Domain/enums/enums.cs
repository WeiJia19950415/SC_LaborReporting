using System;
using System.Collections.Generic;
using System.Text;

namespace SC_LaborReporting.enums
{
    /// <summary>
    /// 审批状态枚举
    /// Submitted = 1,  // 已提交
    /// Approving = 2,  // 审批中
    /// Completed = 3,  // 已完成
    /// Rejected = 4    // 已退回/拒绝
    /// </summary>
    public enum ApprovalStatus
    {
        Submitted = 1,  // 已提交
        Approving = 2,  // 审批中
        Completed = 3,  // 已完成
        Rejected = 4    // 已退回/拒绝
    }

    /// <summary>
    /// 审批记录状态枚举
    /// Pending = 0,    // 未审批
    /// Approved = 1,   // 审批通过
    /// Rejected = 2    // 审批不通过
    /// </summary>
    public enum ApprovalRecordStatus
    {
        Pending = 0,    // 未审批
        Approved = 1,   // 审批通过
        Rejected = 2    // 审批不通过
    }
}
