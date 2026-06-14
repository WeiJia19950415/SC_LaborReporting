using Riok.Mapperly.Abstractions;
using SC_LaborReporting.Books;
using SC_LaborReporting.Departments;
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
public partial class SC_LaborReportingDepartmentToDepartmentDtoMapper : MapperBase<Department, DepartmentDto>
{
    public override partial DepartmentDto Map(Department source);
    public override partial void Map(Department source, DepartmentDto destination);
}

// 2. 将 前端传来的(CreateUpdateDepartmentDto) 转换为 数据库实体(Department)
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class SC_LaborReportingCreateUpdateDepartmentDtoToDepartmentMapper : MapperBase<CreateUpdateDepartmentDto, Department>
{
    public override partial Department Map(CreateUpdateDepartmentDto source);
    public override partial void Map(CreateUpdateDepartmentDto source, Department destination);
}