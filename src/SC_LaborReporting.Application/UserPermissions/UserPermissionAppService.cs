using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;

namespace SC_LaborReporting.UserPermissions
{
    // 添加 [Authorize] 标签，确保只有登录的用户才能访问这个接口
    [Authorize]
    public class UserPermissionAppService : SC_LaborReportingAppService, IUserPermissionAppService
    {
        private readonly IPermissionDefinitionManager _permissionDefinitionManager;
        private readonly IPermissionChecker _permissionChecker;

        // 通过构造函数注入 ABP 框架内置的权限管理器和权限检查器
        public UserPermissionAppService(
            IPermissionDefinitionManager permissionDefinitionManager,
            IPermissionChecker permissionChecker)
        {
            _permissionDefinitionManager = permissionDefinitionManager;
            _permissionChecker = permissionChecker;
        }

        public async Task<List<string>> GetMyPermissionsAsync()
        {
            var grantedPermissions = new List<string>();

            // 1. 获取系统中定义的所有权限
            var allPermissions = await _permissionDefinitionManager.GetPermissionsAsync();

            // 2. 循环判断当前用户是否拥有该权限
            foreach (var permission in allPermissions)
            {
                // 如果用户拥有这个权限，就把它加入到列表中
                if (await _permissionChecker.IsGrantedAsync(permission.Name))
                {
                    grantedPermissions.Add(permission.Name);
                }
            }

            // 3. 返回最终的权限列表
            return grantedPermissions;
        }
    }
}