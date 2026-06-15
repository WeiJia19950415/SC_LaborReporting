using Riok.Mapperly.Abstractions;
using SC_LaborReporting.Books;
using SC_LaborReporting.Departments;
using SC_LaborReporting.LaborCategories;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;

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

