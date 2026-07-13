using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace SC_LaborReporting.ProductSeries;

public interface IProductSeriesAppService : ICrudAppService<
    ProductSeriesDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateProductSeriesDto>
{
}