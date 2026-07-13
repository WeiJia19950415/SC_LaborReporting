using SC_LaborReporting.LaborCategories;
using System;
using Volo.Abp.Domain.Entities;

namespace SC_LaborReporting.LaborReports
{
    public class LaborReportDetail : Entity<Guid>
    {
        public Guid LaborReportId { get; set; }

        public Guid ProductSeriesId { get; set; }
        public Guid LaborCategoryId { get; set; }
        public string LaborCategoryCode { get; set; }
        public decimal Hours { get; set; }
        public double Hoursfinance { get; protected set; }
        public string Jobresponsibilities { get; set; }
        public LaborReportStatus Status { get; set; }
        public LaborClass LaborClass { get; set; } // 1: 项目工时, 2: 非项目工时
        public Guid? ProjectId { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public Guid? ProjectRoleId { get; set; }
        public string ProjectRoleName { get; set; }

        protected LaborReportDetail() { }
        public virtual LaborReport LaborReport { get; protected set; }

        public void SetHoursfinance(double hoursFinance)
        {
            Hoursfinance = hoursFinance;
        }
        public LaborReportDetail(Guid id, Guid laborReportId, Guid laborCategoryId, string laborCategoryCode,
            Guid? projectId, decimal hours, string jobresponsibilities,
            LaborClass laborClass, string projectCode, string projectName, Guid? projectRoleId, string projectRoleName, Guid productSeriesId) : base(id)
        {
            LaborReportId = laborReportId;
            LaborCategoryId = laborCategoryId;
            LaborCategoryCode = laborCategoryCode;
            Hours = hours;
            Jobresponsibilities = jobresponsibilities;
            Status = LaborReportStatus.Pending; // 默认审批中

            ProjectId = projectId;
            LaborClass = laborClass;
            ProjectCode = projectCode;
            ProjectName = projectName;
            ProjectRoleId = projectRoleId;
            ProjectRoleName = projectRoleName;
            ProductSeriesId = productSeriesId;
        }
    }
}