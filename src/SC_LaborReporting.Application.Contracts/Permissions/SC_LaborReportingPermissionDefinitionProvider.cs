using SC_LaborReporting.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace SC_LaborReporting.Permissions;

/// <summary>
/// 权限注册
/// </summary>
public class SC_LaborReportingPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(SC_LaborReportingPermissions.GroupName, L("Permission:SC_LaborReporting"));

        //用户管理
        var users = myGroup.AddPermission(SC_LaborReportingPermissions.User.UserManagement,L("Permission:User"));
        users.AddChild(SC_LaborReportingPermissions.User.UserManagementCreate, L("Permission:User.Create"));
        users.AddChild(SC_LaborReportingPermissions.User.UserManagementUpdate, L("Permission:User.Edit"));
        users.AddChild(SC_LaborReportingPermissions.User.UserManagementDelete, L("Permission:User.Delete"));

        //部门管理
        var Department = myGroup.AddPermission(SC_LaborReportingPermissions.Department.DepartmentManagement, L("Permission:Department"));
        Department.AddChild(SC_LaborReportingPermissions.Department.DepartmentManagementCreate, L("Permission:Department.Create"));
        Department.AddChild(SC_LaborReportingPermissions.Department.DepartmentManagementUpdate, L("Permission:Department.Edit"));
        Department.AddChild(SC_LaborReportingPermissions.Department.DepartmentManagementDelete, L("Permission:Department.Delete"));

        //角色管理
        var Role = myGroup.AddPermission(SC_LaborReportingPermissions.Role.RoleManagement, L("Permission:Role"));
        Role.AddChild(SC_LaborReportingPermissions.Role.RoleManagementCreate, L("Permission:Role.Create"));
        Role.AddChild(SC_LaborReportingPermissions.Role.RoleManagementUpdate, L("Permission:Role.Edit"));
        Role.AddChild(SC_LaborReportingPermissions.Role.RoleManagementDelete, L("Permission:Role.Delete"));


        // Book 模块（你原来的模块）
        var booksPermission = myGroup.AddPermission(SC_LaborReportingPermissions.Books.Default, L("Permission:Books"));
        booksPermission.AddChild(SC_LaborReportingPermissions.Books.Create, L("Permission:Books.Create"));
        booksPermission.AddChild(SC_LaborReportingPermissions.Books.Edit, L("Permission:Books.Edit"));
        booksPermission.AddChild(SC_LaborReportingPermissions.Books.Delete, L("Permission:Books.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<SC_LaborReportingResource>(name);
    }
}
