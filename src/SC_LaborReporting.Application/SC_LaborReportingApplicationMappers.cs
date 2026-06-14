using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;
using SC_LaborReporting.Books;

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
