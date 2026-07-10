using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Uow;


namespace SC_LaborReporting.Users;

[AllowAnonymous] // 暂时放开权限，后续可加上 [Authorize]
public class UserManagementAppService : SC_LaborReportingAppService, IUserManagementAppService
{
    private readonly IdentityUserManager _userManager;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly IOrganizationUnitRepository _organizationUnitRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public UserManagementAppService(
        IdentityUserManager userManager,
        IIdentityUserRepository userRepository,
        IPermissionDefinitionManager permissionDefinitionManager,
        IPermissionChecker permissionChecker,
        IIdentityRoleRepository roleRepository,
            IOrganizationUnitRepository organizationUnitRepository,
            IUnitOfWorkManager unitOfWorkManager)
    {
        _userManager = userManager;
        _userRepository = userRepository;
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionChecker = permissionChecker;
        _roleRepository = roleRepository;
        _organizationUnitRepository = organizationUnitRepository;
        _unitOfWorkManager = unitOfWorkManager;
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
                DepartmentName = ous.FirstOrDefault()?.DisplayName,
                JobNumber = user.GetProperty<string>("JobNumber")
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
        var user = new Volo.Abp.Identity.IdentityUser(GuidGenerator.Create(), input.UserName, input.UserName + "@example.com", CurrentTenant.Id)
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
        user.SetProperty("MustChangePassword", true);
    }

    /// <summary>
    /// 导入用户，如果有失败记录则返回含有失败原因的 Excel 文件流
    /// </summary>
    public async Task<byte[]> ImportUsersAsync(byte[] fileBytes)
    {
        var uowOptions = new AbpUnitOfWorkOptions
        {
            Timeout = 600
        };
        using (var uow = _unitOfWorkManager.Begin(uowOptions))
        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                var rows = stream.Query<UserImportRowDto>().ToList();
                var errorRows = new List<UserImportRowDto>();
                var roleQueryable = await _roleRepository.GetListAsync();
                var ouQueryable = await _organizationUnitRepository.GetListAsync();
                var defaultRoles = roleQueryable.Where(r => r.IsDefault);
                var defaultRoleNames = defaultRoles.Select(r => r.Name).ToList();

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.Name) && string.IsNullOrWhiteSpace(row.PhoneNumber))
                        continue;
                    var pathParts = row.Department?.Split('/').Select(p => p.Trim()).ToList();
                    if (pathParts == null || pathParts.Count == 0)
                    {
                        row.ErrorMessage = "未填写部门";
                        errorRows.Add(row);
                        continue;
                    }
                    Guid? currentParentId = null;
                    OrganizationUnit foundOu = null;
                    bool pathFound = true;
                    foreach (var part in pathParts)
                    {
                        // 在当前父级下查找名称匹配的部门
                        foundOu = ouQueryable.FirstOrDefault(ou =>ou.DisplayName == part && ou.ParentId == currentParentId);

                        if (foundOu == null)
                        {
                            pathFound = false;
                            break;
                        }

                        // 进入下一层：将当前查到的 ID 设为下一层的 ParentId
                        currentParentId = foundOu.Id;
                    }

                    if (!pathFound)
                    {
                        row.ErrorMessage = $"系统中不存在部门路径: {row.Department}";
                        errorRows.Add(row);
                        continue;
                    }
                    var userName = string.IsNullOrWhiteSpace(row.JobNumber) ? row.PhoneNumber : row.JobNumber;
                    var existingUser = await _userManager.FindByNameAsync(userName);
                    if (existingUser != null)
                    {
                        row.ErrorMessage = $"账号({userName})已存在，仅支持新增";
                        errorRows.Add(row);
                        continue;
                    }
                    var user = new Volo.Abp.Identity.IdentityUser(GuidGenerator.Create(), userName, $"{row.PhoneNumber}@demo.com");
                    user.Name = row.Name;
                    user.SetPhoneNumber(row.PhoneNumber, true);

                    if (!string.IsNullOrWhiteSpace(row.JobNumber))
                    {
                        user.SetProperty("JobNumber", row.JobNumber);
                    }
                    var createResult = await _userManager.CreateAsync(user, "SCjg.123000");
                    if (!createResult.Succeeded)
                    {
                        var errorMsg = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        row.ErrorMessage = $"用户创建失败: {errorMsg}";
                        errorRows.Add(row);
                        continue;
                    }
                    if (defaultRoleNames.Any())
                    {
                        await _userManager.AddToRolesAsync(user, defaultRoleNames);
                    }
                    await _userManager.AddToOrganizationUnitAsync(user, foundOu);
                }
                if (errorRows.Any())
                {
                    var outStream = new MemoryStream();
                    outStream.SaveAs(errorRows);
                    return outStream.ToArray();
                }
                // 3. 必须显式提交
                await uow.CompleteAsync();
                return null; // 全部成功
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量导入用户时发生异常");
                throw;
            }
        }
       
    }
}