using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReportDetail : FullAuditedEntity<Guid>
    {
        public Guid LaborReportId { get; set; }
        public Guid LaborCategoryId { get; set; }
        public string LaborCategoryCode { get; set; }
        public Guid? ProjectId { get; set; } // LaborClass=1时必填
        public decimal Hours { get; set; }
        public LaborReportStatus Status { get; set; }

        public string Jobresponsibilities { get; set; } // 工作内容

        public virtual LaborReport LaborReport { get; protected set; }

        protected LaborReportDetail() { }

        public LaborReportDetail(Guid id, Guid reportId, Guid categoryId, string categoryCode, Guid? projectId, decimal hours, string jobresponsibilities)
            : base(id)
        {
            LaborReportId = reportId;
            LaborCategoryId = categoryId;
            LaborCategoryCode = categoryCode;
            ProjectId = projectId;
            Hours = hours;
            Status = LaborReportStatus.Pending; // 默认审核中
            Jobresponsibilities = jobresponsibilities;
        }
    }
}