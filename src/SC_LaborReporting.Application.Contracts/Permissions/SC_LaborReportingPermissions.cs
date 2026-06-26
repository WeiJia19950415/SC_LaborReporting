using static SC_LaborReporting.Permissions.SC_LaborReportingPermissions;

namespace SC_LaborReporting.Permissions;

/// <summary>
/// 权限定义
/// </summary>
public static class SC_LaborReportingPermissions
{
    public const string GroupName = "SC_LaborReporting";


    public static class Books
    {
        public const string Default = GroupName + ".Books";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    /// <summary>
    /// 用户管理
    /// </summary>

    public static class User
    {

        public const string UserManagement = GroupName + ".UserManagement";
        public const string UserManagementCreate = UserManagement + ".Create";
        public const string UserManagementUpdate = UserManagement + ".Update";
        public const string UserManagementDelete = UserManagement + ".Delete";

    }

    /// <summary>
    /// 部门管理
    /// </summary>
    public static class Department
    {
        public const string DepartmentManagement = GroupName + ".DepartmentManagement";
        public const string DepartmentManagementCreate = DepartmentManagement + ".Create";
        public const string DepartmentManagementUpdate = DepartmentManagement + ".Update";
        public const string DepartmentManagementDelete = DepartmentManagement + ".Delete";
    }

    /// <summary>
    /// 角色管理
    /// </summary>
    public static class Role
    {
        public const string RoleManagement = GroupName + ".RoleManagement";
        public const string RoleManagementCreate = RoleManagement + ".Create";
        public const string RoleManagementUpdate = RoleManagement + ".Update";
        public const string RoleManagementDelete = RoleManagement + ".Delete";
    }

    /// <summary>
    /// 报表数据权限
    /// </summary>
    public static class Reports
    {
        public const string ReportManagement = GroupName + ".ReportManagement";
        public const string ReportManagement_BusinessDetails = ReportManagement + ".BusinessDetails";
        public const string ReportManagement_BusinessDetailsALL = ReportManagement + ".BusinessDetailsALL";


        public const string UserHoursReport = ReportManagement + ".UserHoursReport";
        public const string UserFinanceReport = ReportManagement + ".UserFinanceReport";
    }


    /// <summary>
    /// 工时分类管理
    /// </summary>
    public static class LaborCategories
    {
        public const string LaborCategoriesManagement = GroupName + ".LaborCategoriesManagement";
    }

    /// <summary>
    /// 项目管理
    /// </summary>
    public static class Projects
    {
        public const string ProjectsManagement = GroupName + ".Projects";
    }

    /// <summary>
    /// 项目角色管理
    /// </summary>
    public static class ProjectRoles
    {
        public const string ProjectRolesManagement = GroupName + ".ProjectRoles";
    }

    /// <summary>
    /// 工时填报
    /// </summary>
    public static class LaborReport
    {
        public const string LaborReportManagement = GroupName + ".LaborReport";
    }

    /// <summary>
    /// 系统配置
    /// </summary>
    public static class SystemConfig
    {
        public const string SystemConfigManagement = GroupName + ".SystemConfig";

    }

}
