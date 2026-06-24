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

    public static class Reports
    {
        public const string Default = GroupName + ".Reports";
        public const string AllData = Default + ".AllData";
    }

}
