using SC_LaborReporting.enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReportApprovalRecord : AuditedEntity<Guid>
    {
        public Guid LaborReportDetailId { get; set; }

        // 审批级别 (1或2)
        public int Level { get; set; }

        // 指定的审批人ID (通常是部门负责人或上级部门负责人的UserId)
        public Guid ApproverId { get; set; }

        // 审批动作状态
        public ApprovalRecordStatus Status { get; set; }

        // 审批意见
        public string Comment { get; set; }

        // 实际审批时间
        public DateTime? ApprovalTime { get; set; }

        protected LaborReportApprovalRecord() { }

        public LaborReportApprovalRecord(Guid id, Guid laborReportDetailId, int level, Guid approverId) : base(id)
        {
            LaborReportDetailId = laborReportDetailId;
            Level = level;
            ApproverId = approverId;
            Status = ApprovalRecordStatus.Pending;
            Comment = string.Empty;
        }
    }
}