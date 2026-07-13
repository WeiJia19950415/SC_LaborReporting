using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using SC_LaborReporting.Permissions;

namespace SC_LaborReporting.ProductSeries;

public class ProductSeriesAppService : CrudAppService<
    ProductSeries,
    ProductSeriesDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateProductSeriesDto>,
    IProductSeriesAppService
{
    public ProductSeriesAppService(IRepository<ProductSeries, Guid> repository)
        : base(repository)
    {

    }
}