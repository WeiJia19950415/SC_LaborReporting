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

    /// <summary>
    /// 任务分类映射关系
    /// </summary>
    public enum ActivityMappingType
    {
        /// <summary>
        /// 管理活动
        /// </summary>
        Management = 1,

        /// <summary>
        /// 研发活动
        /// </summary>
        RnD = 2,

        /// <summary>
        /// 按照人员部门归属
        /// </summary>
        ByDepartment = 3,

        /// <summary>
        /// 生产活动 (注: 你题目中写的是'生成活动'，通常业务上为'生产活动')
        /// </summary>
        Production = 4,

        /// <summary>
        /// 销售活动
        /// </summary>
        Sales = 5
    }
}
