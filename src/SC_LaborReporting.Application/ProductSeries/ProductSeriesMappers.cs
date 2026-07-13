using Riok.Mapperly.Abstractions;
using SC_LaborReporting.ProductSeries;
using Volo.Abp.Mapperly;

namespace SC_LaborReporting.ProductSeries;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ProductSeriesCreateUpdateMapper : MapperBase<CreateUpdateProductSeriesDto, ProductSeries>
{

    [MapperIgnoreTarget(nameof(ProductSeries.Id))]
    [MapperIgnoreTarget(nameof(ProductSeries.CreationTime))]
    [MapperIgnoreTarget(nameof(ProductSeries.CreatorId))]
    [MapperIgnoreTarget(nameof(ProductSeries.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ProductSeries.LastModifierId))]
    [MapperIgnoreTarget(nameof(ProductSeries.IsDeleted))]
    [MapperIgnoreTarget(nameof(ProductSeries.DeleterId))]
    [MapperIgnoreTarget(nameof(ProductSeries.DeletionTime))]
    public override partial ProductSeries Map(CreateUpdateProductSeriesDto source);

    [MapperIgnoreTarget(nameof(ProductSeries.Id))]
    [MapperIgnoreTarget(nameof(ProductSeries.CreationTime))]
    [MapperIgnoreTarget(nameof(ProductSeries.CreatorId))]
    [MapperIgnoreTarget(nameof(ProductSeries.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ProductSeries.LastModifierId))]
    [MapperIgnoreTarget(nameof(ProductSeries.IsDeleted))]
    [MapperIgnoreTarget(nameof(ProductSeries.DeleterId))]
    [MapperIgnoreTarget(nameof(ProductSeries.DeletionTime))]
    public override partial void Map(CreateUpdateProductSeriesDto source, ProductSeries destination);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public partial class ProductSeriesDtoMapper : MapperBase<ProductSeries, ProductSeriesDto>
{
    public override partial ProductSeriesDto Map(ProductSeries source);

    public override partial void Map(ProductSeries source, ProductSeriesDto destination);
}