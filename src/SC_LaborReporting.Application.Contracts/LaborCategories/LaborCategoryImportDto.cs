using System;

namespace SC_LaborReporting.LaborCategories
{
    public class LaborCategoryImportDto
    {
        public string LaborType { get; set; }
        public string LaborClass { get; set; }
        public string Level1 { get; set; }
        public string Level2 { get; set; }
        public string Level3 { get; set; }
        public string Level4 { get; set; }
        public string Departments { get; set; }
        public string ProjectRoles { get; set; }
        public string Remark { get; set; }
    }
}