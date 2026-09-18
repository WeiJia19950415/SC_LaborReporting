using System;
using System.Collections.Generic;
using System.Text;

namespace SC_LaborReporting.Reports
{
    public class DateColumnInfo
    {
        public string DateStr { get; set; }
        public List<(Guid ProjectId, string ProjectName)> Projects { get; set; }
    }
}
