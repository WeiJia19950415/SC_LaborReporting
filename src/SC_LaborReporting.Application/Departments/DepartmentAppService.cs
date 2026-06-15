using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace SC_LaborReporting.Departments;

[AllowAnonymous]
public class DepartmentAppService : SC_LaborReportingAppService, IDepartmentAppService
{
    private readonly OrganizationUnitManager _organizationUnitManager;
    private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IIdentityUserRepository _identityUserRepository; // 注入UserRepository用于查负责人姓名

    public DepartmentAppService(
        OrganizationUnitManager organizationUnitManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository,
        IdentityUserManager identityUserManager,
        IIdentityUserRepository identityUserRepository)
    {
        _organizationUnitManager = organizationUnitManager;
        _organizationUnitRepository = organizationUnitRepository;
        _identityUserManager = identityUserManager;
        _identityUserRepository = identityUserRepository;
    }

    public async Task<ListResultDto<DepartmentDto>> GetListAsync()
    {
        var list = await _organizationUnitRepository.GetListAsync();
        // 按照 Code 排序，这是 ABP 设计的精髓：保证父节点一定排在子节点前面
        list.Sort((x, y) => string.Compare(x.Code, y.Code, StringComparison.Ordinal));

        var dtoList = new List<DepartmentDto>();
        var idToFullNameMap = new Dictionary<Guid, string>(); // 用于缓存层级全称

        // 预先查询所有用户（也可以按需查询，这里为了性能拉取列表）
        var allUsers = await _identityUserRepository.GetListAsync();
        var userDict = allUsers.ToDictionary(u => u.Id, u => u.Name);

        foreach (var ou in list)
        {
            var dto = ObjectMapper.Map<OrganizationUnit, DepartmentDto>(ou);

            // 1. 动态生成部门全称：上级全称 + "-" + 当前名称
            string fullName = ou.DisplayName;
            if (ou.ParentId.HasValue && idToFullNameMap.TryGetValue(ou.ParentId.Value, out var parentFullName))
            {
                fullName = $"{parentFullName}-{ou.DisplayName}";
            }
            idToFullNameMap[ou.Id] = fullName; // 存入缓存供子节点拼接
            dto.FullName = fullName;

            // 2. 读取扩展属性 (ABP 会自动从 ExtraProperties 列中解析)
            dto.DepartmentType = ou.GetProperty("DepartmentType", DepartmentType.Department);
            dto.ManagerId = ou.GetProperty<Guid?>("ManagerId");

            // 3. 映射负责人姓名
            if (dto.ManagerId.HasValue && userDict.TryGetValue(dto.ManagerId.Value, out var mName))
            {
                dto.ManagerName = mName;
            }

            dtoList.Add(dto);
        }

        return new ListResultDto<DepartmentDto>(dtoList);
    }

    public async Task<DepartmentDto> GetAsync(Guid id)
    {
        // ...与上述同理，为了节省篇幅这里暂略...
        // 前端树形菜单一般直接走 GetListAsync
        throw new NotImplementedException();
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentInput input)
    {
        var ou = new OrganizationUnit(GuidGenerator.Create(), input.DisplayName, input.ParentId, CurrentTenant.Id);

        // 存储扩展属性
        ou.SetProperty("DepartmentType", input.DepartmentType);
        ou.SetProperty("ManagerId", input.ManagerId);

        await _organizationUnitManager.CreateAsync(ou);
        await CurrentUnitOfWork.SaveChangesAsync();

        return ObjectMapper.Map<OrganizationUnit, DepartmentDto>(ou);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentInput input)
    {
        var ou = await _organizationUnitRepository.GetAsync(id);
        ou.DisplayName = input.DisplayName;

        // 更新扩展属性
        ou.SetProperty("DepartmentType", input.DepartmentType);
        ou.SetProperty("ManagerId", input.ManagerId);

        await _organizationUnitManager.UpdateAsync(ou);
        return ObjectMapper.Map<OrganizationUnit, DepartmentDto>(ou);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _organizationUnitManager.DeleteAsync(id);
    }

    public async Task<ListResultDto<IdentityUserDto>> GetUsersByDepartmentIdAsync(Guid departmentId)
    {
        // 之前的逻辑保持不变
        var users = await _identityUserRepository.GetUsersInOrganizationUnitAsync(departmentId);
        return new ListResultDto<IdentityUserDto>(
            ObjectMapper.Map<List<IdentityUser>, List<IdentityUserDto>>(users)
        );
    }
}