using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Identity;

namespace SC_LaborReporting.Users;

[AllowAnonymous] // 暂时放开权限，后续可加上 [Authorize]
public class UserManagementAppService : SC_LaborReportingAppService, IUserManagementAppService
{
    private readonly IdentityUserManager _userManager;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionChecker _permissionChecker;

    public UserManagementAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionChecker permissionChecker)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionChecker = permissionChecker;
    }

    public async Task<PagedResultDto<UserDetailDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var users = await _userRepository.GetListAsync(input.Sorting, input.MaxResultCount, input.SkipCount);
        
        var totalCount = await _userRepository.GetCountAsync();

        var dtoList = new List<UserDetailDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var ous = await _userManager.GetOrganizationUnitsAsync(user); // 获取部门

            dtoList.Add(new UserDetailDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                RoleNames = roles.ToArray(),
                DepartmentId = ous.FirstOrDefault()?.Id,
                DepartmentName = ous.FirstOrDefault()?.DisplayName
            });
        }

        return new PagedResultDto<UserDetailDto>(totalCount, dtoList);
    }

    public async Task<UserDetailDto> GetAsync(Guid id)
    {
        var user = await _userManager.GetByIdAsync(id);
        var roles = await _userManager.GetRolesAsync(user);
        var ous = await _userManager.GetOrganizationUnitsAsync(user);

        return new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            RoleNames = roles.ToArray(),
            DepartmentId = ous.FirstOrDefault()?.Id,
            DepartmentName = ous.FirstOrDefault()?.DisplayName
        };
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserInput input)
    {
        var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.UserName + "@example.com", CurrentTenant.Id)
        {
            Name = input.Name
        };
        user.SetPhoneNumber(input.PhoneNumber, true);
        user.SetIsActive(input.IsActive);

        (await _userManager.CreateAsync(user, input.Password)).CheckErrors();

        if (input.RoleNames?.Any() == true)
            (await _userManager.SetRolesAsync(user, input.RoleNames)).CheckErrors();

        if (input.DepartmentId.HasValue)
            await _userManager.SetOrganizationUnitsAsync(user, new[] { input.DepartmentId.Value });

        return await GetAsync(user.Id);
    }

    public async Task UpdateAsync(Guid id, UpdateUserInput input)
    {
        var user = await _userManager.GetByIdAsync(id);

        // 后端严格控制：不更新 UserName, Name, PhoneNumber
        user.SetIsActive(input.IsActive);
        (await _userManager.UpdateAsync(user)).CheckErrors();

        // 更新角色
        if (input.RoleNames != null)
            (await _userManager.SetRolesAsync(user, input.RoleNames)).CheckErrors();

        // 更新部门 (一对多逻辑：清除所有旧部门，加入新部门)
        var currentOus = await _userManager.GetOrganizationUnitsAsync(user);
        foreach (var ou in currentOus)
            await _userManager.RemoveFromOrganizationUnitAsync(user.Id, ou.Id);

        if (input.DepartmentId.HasValue)
            await _userManager.AddToOrganizationUnitAsync(user.Id, input.DepartmentId.Value);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _userManager.GetByIdAsync(id);
        (await _userManager.DeleteAsync(user)).CheckErrors();
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

    public async Task ResetPasswordAsync(Guid id)
    {
        var user = await _userManager.GetByIdAsync(id);

        // 生成重置Token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // 强制重置为默认密码，ABP会自动检查密码复杂度 (SCjg.123000 满足默认规则)
        (await _userManager.ResetPasswordAsync(user, token, "SCjg.123000")).CheckErrors();
    }
}