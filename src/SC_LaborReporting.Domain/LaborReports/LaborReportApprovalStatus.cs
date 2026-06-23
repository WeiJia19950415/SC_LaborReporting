using SC_LaborReporting.enums;
using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReportApprovalStatus : AuditedEntity<Guid>
    {
        public Guid LaborReportDetailId { get; set; }

        // 整体审批状态
        public ApprovalStatus Status { get; set; }

        // 当前处于第几级审批 (1=部门负责人，2=上级部门负责人)
        public int CurrentLevel { get; set; }

        protected LaborReportApprovalStatus() { }

        public LaborReportApprovalStatus(Guid id, Guid laborReportDetailId) : base(id)
        {
            LaborReportDetailId = laborReportDetailId;
            Status = ApprovalStatus.Submitted;
            CurrentLevel = 1; // 默认从第一级开始
        }
    }
}