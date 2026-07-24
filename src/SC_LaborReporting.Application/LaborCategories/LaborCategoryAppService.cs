using Microsoft.AspNetCore.Authorization;
using SC_LaborReporting.ProjectRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace SC_LaborReporting.LaborCategories;

[AllowAnonymous]
public class LaborCategoryAppService : SC_LaborReportingAppService, ILaborCategoryAppService
{
    private readonly IRepository<LaborCategory, Guid> _repository;
    private readonly IRepository<OrganizationUnit, Guid> _ouRepository; // 注入部门仓储用于查全称
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<ProjectRole, Guid> _projectRoleRepository;

    public LaborCategoryAppService(
        IRepository<LaborCategory, Guid> repository,
        IRepository<OrganizationUnit, Guid> ouRepository,
        IdentityUserManager userManager,
        IRepository<ProjectRole, Guid> projectRoleRepository)
    {
        _repository = repository;
        _ouRepository = ouRepository;
        _userManager = userManager;
        _projectRoleRepository = projectRoleRepository;
    }
    public async Task<List<LaborCategoryDto>> GetLeafCategoriesAsync(Guid? projectRoleId, Guid? departmentId, LaborClass laborClass)
    {
        var currentUserId = CurrentUser.GetId();
        var user = await _userManager.GetByIdAsync(currentUserId);
        var ous = await _userManager.GetOrganizationUnitsAsync(user);
        departmentId = ous.FirstOrDefault()?.Id;
        var query = await _repository.WithDetailsAsync(x => x.Departments, x => x.ProjectRoles);
        var parentIds = query.Where(x => x.ParentId != null).Select(x => x.ParentId.Value).Distinct().ToList();
        var leafQuery = query.Where(x => !parentIds.Contains(x.Id) && x.LaborClass == laborClass);
        if (departmentId.HasValue)
        {
            leafQuery = leafQuery.Where(x => x.Departments.Any(d => d.DepartmentId == departmentId.Value) || x.Departments.Count == 0);
        }
        if (projectRoleId.HasValue)
        {
            leafQuery = leafQuery.Where(x => x.ProjectRoles.Any(r => r.ProjectRoleId == projectRoleId.Value) || x.ProjectRoles.Count == 0);
        }
        var leafNodes = await AsyncExecuter.ToListAsync(leafQuery);
        var dtos = ObjectMapper.Map<List<LaborCategory>, List<LaborCategoryDto>>(leafNodes);
        var allCategories = await _repository.GetListAsync();
        var categoryDict = allCategories.ToDictionary(x => x.Id);
        
        foreach (var dto in dtos)
        {
            var hierarchyNames = new List<string>();
            Guid? currentId = dto.Id;
            while (currentId.HasValue && categoryDict.TryGetValue(currentId.Value, out var currentCategory))
            {
                hierarchyNames.Insert(0, currentCategory.Name);
                currentId = currentCategory.ParentId;
            }
            dto.FullName = string.Join(" - ", hierarchyNames);
        }
        return dtos;
    }

    public async Task<ListResultDto<LaborCategoryDto>> GetListAsync()
    {

        var query = await _repository.WithDetailsAsync(x => x.Departments, x => x.ProjectRoles);
        var categories = query.ToList();
        categories.Sort((x, y) => string.Compare(x.Code, y.Code, StringComparison.Ordinal));


        var allOus = await _ouRepository.GetListAsync();
        var ouDict = allOus.ToDictionary(x => x.Id, x => x);


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
            dto.DepartmentIds = item.Departments.Select(d => d.DepartmentId).ToArray();
            dto.ProjectRoleIds = item.ProjectRoles.Select(r => r.ProjectRoleId).ToArray();
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

        if (input.ParentId == id) throw new Exception("上级分类不能是当前分类本身！");


        var query = await _repository.WithDetailsAsync(x => x.Departments, x => x.ProjectRoles);
        var category = query.FirstOrDefault(x => x.Id == id);

        category.LaborType = input.LaborType;
        category.LaborClass = input.LaborClass;
        category.Name = input.Name;
        category.Remark = input.Remark;

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

        var existingDeptIds = category.Departments.Select(d => d.DepartmentId).ToList();
        var deptsToAdd = currentDeptIds.Where(deptId => !existingDeptIds.Contains(deptId)).ToList();
        foreach (var deptId in deptsToAdd)
        {
            category.Departments.Add(new LaborCategoryDepartment(GuidGenerator.Create(), category.Id, deptId));
        }

        var currentRoles = input.ProjectRoleIds;

        var rolesToRemove = category.ProjectRoles.Where(r => !currentRoles.Contains(r.ProjectRoleId)).ToList();
        foreach (var role in rolesToRemove)
        {
            category.ProjectRoles.Remove(role);
        }

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


    public async Task ImportAsync(List<LaborCategoryImportDto> inputs)
    {
        // 构建部门全称字典 (如: 事业一部-研发-软件组 -> Guid)
        var allOus = await _ouRepository.GetListAsync();
        var ouFullNameDict = new Dictionary<string, Guid>();
        foreach (var ou in allOus)
        {
            var pathNames = new List<string>();
            var current = ou;
            while (current != null)
            {
                pathNames.Insert(0, current.DisplayName);
                current = current.ParentId.HasValue ? allOus.FirstOrDefault(x => x.Id == current.ParentId.Value) : null;
            }
            ouFullNameDict[string.Join("-", pathNames)] = ou.Id;
        }

        // 构建角色字典 (如: 软件开发组 -> Guid)
        var allRoles = await _projectRoleRepository.GetListAsync();
        var roleDict = allRoles.ToDictionary(x => x.Name, x => x.Id);

        // 记录 Excel 树形层级的状态
        string lastType = null, lastClass = null;
        string lastL1 = null, lastL2 = null, lastL3 = null, lastL4 = null;

        foreach (var row in inputs)
        {
            if (!string.IsNullOrWhiteSpace(row.LaborType)) lastType = row.LaborType;
            if (!string.IsNullOrWhiteSpace(row.LaborClass)) lastClass = row.LaborClass;

            if (!string.IsNullOrWhiteSpace(row.Level1)) { lastL1 = row.Level1; lastL2 = null; lastL3 = null; lastL4 = null; }
            if (!string.IsNullOrWhiteSpace(row.Level2)) { lastL2 = row.Level2; lastL3 = null; lastL4 = null; }
            if (!string.IsNullOrWhiteSpace(row.Level3)) { lastL3 = row.Level3; lastL4 = null; }
            if (!string.IsNullOrWhiteSpace(row.Level4)) { lastL4 = row.Level4; }

            var t = lastType?.Trim() == "生产工时" ? LaborType.Production : LaborType.RAndD;
            var c = lastClass?.Trim() == "其他工时" ? LaborClass.Other : LaborClass.Project;

            var deptIds = ParseNamesToIds(row.Departments, ouFullNameDict);
            var roleIds = ParseNamesToIds(row.ProjectRoles, roleDict);

            Guid? parentId = null;

            // 逐级处理，存在则更新，不存在则新增
            if (!string.IsNullOrWhiteSpace(lastL1))
            {
                bool isLeaf = string.IsNullOrWhiteSpace(lastL2);
                var l1Node = await CreateOrUpdateNodeAsync(lastL1.Trim(), null, t, c, row.Remark, deptIds, roleIds, isLeaf);
                parentId = l1Node.Id;

                if (!isLeaf && !string.IsNullOrWhiteSpace(lastL2))
                {
                    isLeaf = string.IsNullOrWhiteSpace(lastL3);
                    var l2Node = await CreateOrUpdateNodeAsync(lastL2.Trim(), parentId, t, c, row.Remark, deptIds, roleIds, isLeaf);
                    parentId = l2Node.Id;

                    if (!isLeaf && !string.IsNullOrWhiteSpace(lastL3))
                    {
                        isLeaf = string.IsNullOrWhiteSpace(lastL4);
                        var l3Node = await CreateOrUpdateNodeAsync(lastL3.Trim(), parentId, t, c, row.Remark, deptIds, roleIds, isLeaf);
                        parentId = l3Node.Id;

                        if (!isLeaf && !string.IsNullOrWhiteSpace(lastL4))
                        {
                            var l4Node = await CreateOrUpdateNodeAsync(lastL4.Trim(), parentId, t, c, row.Remark, deptIds, roleIds, true);
                            parentId = l4Node.Id;
                        }
                    }
                }
            }
        }
    }


    private async Task<LaborCategory> CreateOrUpdateNodeAsync(
        string name, Guid? parentId,
        LaborType type, LaborClass cls,
        string remark,
        List<Guid> deptIds,
        List<Guid> roleIds,
        bool isLeaf)
    {
        // 包含导航属性的查询，用于更新部门和角色关系
        var query = await _repository.WithDetailsAsync(x => x.Departments, x => x.ProjectRoles);
        // 关键逻辑：仅通过 名称 + 父级ID 判断是否存在
        var node = query.FirstOrDefault(x => x.Name == name && x.ParentId == parentId);

        if (node != null)
        {
            bool isModified = false;
            if (node.LaborType != type) { node.LaborType = type; isModified = true; }
            if (node.LaborClass != cls) { node.LaborClass = cls; isModified = true; }
            if (isLeaf)
            {
                var safeRemark = remark ?? "";
                if (node.Remark != safeRemark)
                {
                    node.Remark = safeRemark;
                    isModified = true;
                }
                var existingDeptIds = node.Departments.Select(d => d.DepartmentId).ToList();
                var deptToAdd = deptIds.Except(existingDeptIds).ToList();
                var deptToRemove = existingDeptIds.Except(deptIds).ToList();
                if (deptToRemove.Any())
                {
                    node.Departments.RemoveAll(d => deptToRemove.Contains(d.DepartmentId));
                    isModified = true;
                }
                foreach (var dId in deptToAdd)
                {
                    node.Departments.Add(new LaborCategoryDepartment(GuidGenerator.Create(), node.Id, dId));
                    isModified = true;
                }
                var existingRoleIds = node.ProjectRoles.Select(r => r.ProjectRoleId).ToList();
                var roleToAdd = roleIds.Except(existingRoleIds).ToList();
                var roleToRemove = existingRoleIds.Except(roleIds).ToList();
                if (roleToRemove.Any())
                {
                    node.ProjectRoles.RemoveAll(r => roleToRemove.Contains(r.ProjectRoleId));
                    isModified = true;
                }
                foreach (var rId in roleToAdd)
                {
                    node.ProjectRoles.Add(new LaborCategoryProjectRole { LaborCategoryId = node.Id, ProjectRoleId = rId });
                    isModified = true;
                }
            }

            if (isModified)
            {
                await _repository.UpdateAsync(node);
                await CurrentUnitOfWork.SaveChangesAsync(); // 强制提交
            }

            return node;
        }
        else
        {
            var input = new CreateUpdateLaborCategoryInput
            {
                Name = name,
                ParentId = parentId,
                LaborType = type,
                LaborClass = cls,
                Remark = isLeaf ? (remark ?? "") : "" // 防止 NULL 报错
            };

            var dto = await CreateAsync(input);
            await CurrentUnitOfWork.SaveChangesAsync(); // 强制提交保存，生成主键

            // 重新查出该实体以插入子表关系
            var newQuery = await _repository.WithDetailsAsync(x => x.Departments, x => x.ProjectRoles);
            var createdNode = newQuery.FirstOrDefault(x => x.Id == dto.Id);

            if (isLeaf && createdNode != null)
            {
                if (deptIds != null && deptIds.Any())
                {
                    foreach (var dId in deptIds)
                    {
                        createdNode.Departments.Add(new LaborCategoryDepartment(GuidGenerator.Create(), createdNode.Id, dId));
                    }
                }
                if (roleIds != null && roleIds.Any())
                {
                    foreach (var rId in roleIds)
                    {
                        createdNode.ProjectRoles.Add(new LaborCategoryProjectRole { LaborCategoryId = createdNode.Id, ProjectRoleId = rId });
                    }
                }
                await _repository.UpdateAsync(createdNode);
                await CurrentUnitOfWork.SaveChangesAsync();
            }

            return createdNode ?? await _repository.GetAsync(dto.Id);
        }
    }

    private List<Guid> ParseNamesToIds(string str, Dictionary<string, Guid> dict)
    {
        var ids = new List<Guid>();
        if (string.IsNullOrWhiteSpace(str)) return ids;

        var names = str.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in names)
        {
            if (dict.TryGetValue(name.Trim(), out var id))
            {
                ids.Add(id);
            }
        }
        return ids;
    }
}