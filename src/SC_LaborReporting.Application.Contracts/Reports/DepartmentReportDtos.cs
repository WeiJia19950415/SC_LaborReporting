using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace SC_LaborReporting.Reports
{
    public class DepartmentReportQueryDto : PagedAndSortedResultRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IncludeUnapproved { get; set; }
        public Guid? ProjectId { get; set; }
        public bool FilterByProject { get; set; }
        public Guid? LaborCategoryId { get; set; }
    }

    public class ChartDataDto
    {
        public string Name { get; set; }
        public decimal Value { get; set; }
        public Guid? ReferenceId { get; set; }
    }

    public class DepartmentReportDetailDto
    {
        public string LaborClass { get; set; }
        public string ProjectName { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectRoleName { get; set; }
        public string ReporterName { get; set; }
        public string DepartmentFullName { get; set; }
        public string LaborCategoryFullName { get; set; }
        public string Jobresponsibilities { get; set; }
        public decimal Hours { get; set; }
        public int Status { get; set; }
    }

    public class ExportFileDto
    {
        public string FileName { get; set; }
        public byte[] Content { get; set; }
    }
}
