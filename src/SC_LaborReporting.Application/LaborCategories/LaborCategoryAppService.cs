using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace SC_LaborReporting.LaborCategories;

[AllowAnonymous]
public class LaborCategoryAppService : SC_LaborReportingAppService, ILaborCategoryAppService
{
    private readonly IRepository<LaborCategory, Guid> _repository;
    private readonly IRepository<OrganizationUnit, Guid> _ouRepository; // 注入部门仓储用于查全称

    public LaborCategoryAppService(
        IRepository<LaborCategory, Guid> repository,
        IRepository<OrganizationUnit, Guid> ouRepository)
    {
        _repository = repository;
        _ouRepository = ouRepository;
    }

    public async Task<ListResultDto<LaborCategoryDto>> GetListAsync()
    {
        // ⭐ 1. 贪婪加载关联子表
        var query = await _repository.WithDetailsAsync(x => x.Departments, x => x.ProjectRoles);
        var categories = query.ToList();
        categories.Sort((x, y) => string.Compare(x.Code, y.Code, StringComparison.Ordinal));

        // ⭐ 2. 预加载所有部门，用于动态计算全称 (避免 N+1 查询问题)
        var allOus = await _ouRepository.GetListAsync();
        var ouDict = allOus.ToDictionary(x => x.Id, x => x);

        // 递归获取全称的本地方法
        string GetDepartmentFullName(Guid id)
        {
            if (!ouDict.ContainsKey(id)) return "";
            var ou = ouDict[id];
            var fullName = ou.DisplayName;
            while (ou.ParentId.HasValue && ouDict.ContainsKey(ou.ParentId.Value))
            {
                ou = ouDict[ou.ParentId.Value];
                fullName = $"{ou.DisplayName}-{fullName}";
            }
            return fullName;
        }

        var dtoList = new List<LaborCategoryDto>();
        foreach (var item in categories)
        {
            var dto = ObjectMapper.Map<LaborCategory, LaborCategoryDto>(item);

            // 手动映射子表数据
            dto.DepartmentIds = item.Departments.Select(d => d.DepartmentId).ToArray();
            dto.ProjectRoleIds = item.ProjectRoles.Select(r => r.ProjectRoleId).ToArray();

            // ⭐ 3. 动态组装部门全称
            dto.DepartmentFullNames = dto.DepartmentIds.Select(id => GetDepartmentFullName(id)).ToArray();

            dtoList.Add(dto);
        }

        return new ListResultDto<LaborCategoryDto>(dtoList);
    }

    public async Task<LaborCategoryDto> CreateAsync(CreateUpdateLaborCategoryInput input)
    {
        var category = new LaborCategory
        {
            LaborType = input.LaborType,
            LaborClass = input.LaborClass,
            Name = input.Name,
            ParentId = input.ParentId,
            Remark = input.Remark
        };

        category.Code = await GetNextChildCodeAsync(input.ParentId);

        // ⭐ 插入关系表
        if (input.DepartmentIds != null)
        {
            foreach (var deptId in input.DepartmentIds)
                category.Departments.Add(new LaborCategoryDepartment(GuidGenerator.Create(), category.Id, deptId));
        }
        if (input.ProjectRoleIds != null)
        {
            foreach (var roleId in input.ProjectRoleIds)
            {
                category.ProjectRoles.Add(new LaborCategoryProjectRole { LaborCategoryId = category.Id, ProjectRoleId = roleId });
            }
        }

        await _repository.InsertAsync(category);
        return ObjectMapper.Map<LaborCategory, LaborCategoryDto>(category);
    }

    public async Task<LaborCategoryDto> UpdateAsync(Guid id, CreateUpdateLaborCategoryInput input)
    {
        // 防护：自关联不能是自己
        if (input.ParentId == id) throw new Exception("上级分类不能是当前分类本身！");

        // 使用 IncludeDetails 获取带有子表的数据
        var query = await _repository.WithDetailsAsync(x => x.Departments, x => x.ProjectRoles);
        var category = query.FirstOrDefault(x => x.Id == id);

        category.LaborType = input.LaborType;
        category.LaborClass = input.LaborClass;
        category.Name = input.Name;
        category.Remark = input.Remark;

        // 如果修改了上级分类，重新生成 Code
        if (category.ParentId != input.ParentId)
        {
            category.ParentId = input.ParentId;
            category.Code = await GetNextChildCodeAsync(input.ParentId);
        }

        var currentDeptIds = input.DepartmentIds ?? Array.Empty<Guid>();

        var deptsToRemove = category.Departments.Where(d => !currentDeptIds.Contains(d.DepartmentId)).ToList();
        foreach (var dept in deptsToRemove)
        {
            category.Departments.Remove(dept);
        }

        // 2. 添加前端新增的项 (利用刚刚写的构造函数，并通过 GuidGenerator.Create() 生成唯一主键)
        var existingDeptIds = category.Departments.Select(d => d.DepartmentId).ToList();
        var deptsToAdd = currentDeptIds.Where(deptId => !existingDeptIds.Contains(deptId)).ToList();
        foreach (var deptId in deptsToAdd)
        {
            category.Departments.Add(new LaborCategoryDepartment(GuidGenerator.Create(), category.Id, deptId));
        }

        var currentRoles = input.ProjectRoleIds;
        // 1. 删除前端已取消勾选的项
        var rolesToRemove = category.ProjectRoles.Where(r => !currentRoles.Contains(r.ProjectRoleId)).ToList();
        foreach (var role in rolesToRemove)
        {
            category.ProjectRoles.Remove(role);
        }

        // 2. 添加前端新增的项
        var existingRoles = category.ProjectRoles.Select(r => r.ProjectRoleId).ToList();
        var rolesToAdd = currentRoles.Where(r => !existingRoles.Contains(r)).ToList();
        foreach (var roleName in rolesToAdd)
        {
            category.ProjectRoles.Add(new LaborCategoryProjectRole() { ProjectRoleId= roleName});
        }

        await _repository.UpdateAsync(category);
        return ObjectMapper.Map<LaborCategory, LaborCategoryDto>(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        var childrenCount = await _repository.CountAsync(x => x.ParentId == id);
        if (childrenCount > 0) throw new Exception("存在下级分类，无法删除");

        // EF Core 配置了级联删除或在 Repository 中删除时会自动清理关联表
        await _repository.DeleteAsync(id);
    }
    private async Task<string> GetNextChildCodeAsync(Guid? parentId)
    {
        var lastChild = (await _repository.GetQueryableAsync())
            .Where(x => x.ParentId == parentId)
            .OrderByDescending(x => x.Code)
            .FirstOrDefault();

        var nextCode = lastChild == null ? "001" : GenerateNextCode(lastChild.Code.Split('.').Last());

        if (parentId.HasValue)
        {
            var parent = await _repository.GetAsync(parentId.Value);
            return $"{parent.Code}.{nextCode}";
        }
        return nextCode;
    }

    private string GenerateNextCode(string lastCode)
    {
        if (int.TryParse(lastCode, out int number))
        {
            return (number + 1).ToString(new string('0', lastCode.Length));
        }
        return "001";
    }
}