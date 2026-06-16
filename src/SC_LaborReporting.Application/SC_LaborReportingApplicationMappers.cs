using Riok.Mapperly.Abstractions;
using SC_LaborReporting.Books;
using SC_LaborReporting.Departments;
using SC_LaborReporting.LaborCategories;
using SC_LaborReporting.LaborReports;
using SC_LaborReporting.Projects;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using SC_LaborReporting.ProjectRoles;

namespace SC_LaborReporting;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingBookToBookDtoMapper : MapperBase<Book, BookDto>
{
    public override partial BookDto Map(Book source);

    public override partial void Map(Book source, BookDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingCreateUpdateBookDtoToBookMapper : MapperBase<CreateUpdateBookDto, Book>
{
    public override partial Book Map(CreateUpdateBookDto source);

    public override partial void Map(CreateUpdateBookDto source, Book destination);
}

// 1. 将 数据库实体(Department) 转换为 返回前端的(DepartmentDto)
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingDepartmentToDepartmentDtoMapper : MapperBase<OrganizationUnit, DepartmentDto>
{
    public override partial DepartmentDto Map(OrganizationUnit source);
    public override partial void Map(OrganizationUnit source, DepartmentDto destination);
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingLaborCategorytoLaborCategoryDtoMapper : MapperBase<LaborCategory, LaborCategoryDto>
{
    public override partial LaborCategoryDto Map(LaborCategory source);
    public override partial void Map(LaborCategory source, LaborCategoryDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingProjectToProjectDtoMapper : MapperBase<Project, ProjectDto>
{
    public override partial ProjectDto Map(Project source);
    public override partial void Map(Project source, ProjectDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingCreateUpdateProjectDtoToProjectMapper : MapperBase<CreateUpdateProjectDto, Project>
{
    public override partial Project Map(CreateUpdateProjectDto source);
    public override partial void Map(CreateUpdateProjectDto source, Project destination);
}


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingLaborReportMapper : MapperBase<LaborReportDetail, LaborReportItemDto>
{
    public override partial LaborReportItemDto Map(LaborReportDetail source);
    public override partial void Map(LaborReportDetail source, LaborReportItemDto destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingProjectRoleMapper : MapperBase<ProjectRole, ProjectRoleDto>
{
    public override partial ProjectRoleDto Map(ProjectRole source);
    public override partial void Map(ProjectRole source, ProjectRoleDto destination);
}
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingCreateProjectRoleMapper : MapperBase<CreateUpdateProjectRoleDto, ProjectRole>
{
    // 1. 用于新增：创建一个新对象
    public override partial ProjectRole Map(CreateUpdateProjectRoleDto source);

    // 2. 用于修改：将 DTO 的值覆盖到已存在的实体上
    public override partial void Map(CreateUpdateProjectRoleDto source, ProjectRole destination);
}