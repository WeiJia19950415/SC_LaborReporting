using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReport : FullAuditedAggregateRoot<Guid>
    {
        public Guid ReporterId { get; set; }
        public Guid DepartmentId { get; set; }
        /// <summary>
        /// 当天有效工时总和：审批通过的工时记录的小时数总和
        /// </summary>
        public decimal TotalEffectiveHours { get; private set; }
        /// <summary>
        /// 当天加班工时总和：当天有效工时超过8小时的部分，例如有效工时为9小时则加班工时为1小时，7小时则加班工时为0小时
        /// </summary>
        public decimal TotalOvertimeHours { get; private set; }
        /// <summary>
        /// 表示这条工时记录是针对哪一天的工时进行的汇报
        /// </summary>
        public DateTime ReportDate { get; set; }
        /// <summary>
        /// 工作详情
        /// </summary>
        public virtual ICollection<LaborReportDetail> Details { get; protected set; }

        protected LaborReport() { }

        public LaborReport(Guid id, Guid reporterId, Guid departmentId, DateTime reportDate)
            : base(id)
        {
            ReporterId = reporterId;
            DepartmentId = departmentId;
            ReportDate = reportDate;
            Details = new List<LaborReportDetail>();
            TotalEffectiveHours = 0;
            TotalOvertimeHours = 0;
        }

        // 根据审批完成的从表记录，重新计算主表的工时
        public void RecalculateHours()
        {
            TotalEffectiveHours = Details
                .Where(x => x.Status == LaborReportStatus.Approved)
                .Sum(x => x.Hours);

            // 发生加班工时：超出8的部分，例如有效为9该值为1，7则为0
            TotalOvertimeHours = TotalEffectiveHours > 8m ? TotalEffectiveHours - 8m : 0m;
        }
    }
}