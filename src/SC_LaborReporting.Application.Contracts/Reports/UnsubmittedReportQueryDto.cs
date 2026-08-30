using System;
using System.Collections.Generic;
using System.Text;

namespace SC_LaborReporting.Reports
{
    public class UnsubmittedReportQueryDto
    {
        public DateTime QueryDate { get; set; }
        public Guid? DepartmentId { get; set; }
    }

    public class UnsubmittedUserDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string DepartmentName { get; set; }
    }
}
