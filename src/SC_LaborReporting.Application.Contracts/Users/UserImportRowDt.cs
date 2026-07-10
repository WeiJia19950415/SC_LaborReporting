using MiniExcelLibs.Attributes;

namespace SC_LaborReporting.Users
{
    public class UserImportRowDto
    {
        [ExcelColumn(Name = "姓名")]
        public string Name { get; set; }

        [ExcelColumn(Name = "电话号码")]
        public string PhoneNumber { get; set; }

        [ExcelColumn(Name = "所属部门")]
        public string Department { get; set; }

        [ExcelColumn(Name = "工号")]
        public string JobNumber { get; set; }

        // 此列用于在导出失败记录时，给用户提示失败原因
        [ExcelColumn(Name = "失败原因", Width = 40)]
        public string ErrorMessage { get; set; }
    }
}