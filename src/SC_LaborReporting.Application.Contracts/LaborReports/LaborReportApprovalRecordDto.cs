using System;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReportApprovalRecordDto
    {
        public Guid Id { get; set; }
        public Guid LaborReportId { get; set; } // 关联的工时申报ID或详情ID
        public string ApproverName { get; set; } // 审批人姓名
        public string ApprovalNode { get; set; } // 审批节点 (如：项目经理审批、部门主管审批)
        public string ApprovalStatus { get; set; } // 审批状态 (如：通过、驳回)
        public string ApprovalContent { get; set; } // 审批内容/意见
        public DateTime CreationTime { get; set; } // 审批时间
    }
}