using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;

namespace SC_LaborReporting.Departments;

public interface IDepartmentAppService : IApplicationService
{
    // 部门 CRUD
    Task<DepartmentDto> GetAsync(Guid id);
    Task<ListResultDto<DepartmentDto>> GetListAsync(); // 获取平铺列表（前端通过 Code 或 ParentId 自行组装树）
    Task<DepartmentDto> CreateAsync(CreateDepartmentInput input);
    Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentInput input);
    Task DeleteAsync(Guid id);

    // 查询部门下的员工
    Task<ListResultDto<IdentityUserDto>> GetUsersByDepartmentIdAsync(Guid departmentId);
}