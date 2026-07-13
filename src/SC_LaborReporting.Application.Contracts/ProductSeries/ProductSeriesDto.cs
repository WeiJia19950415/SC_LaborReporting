using System;
using Volo.Abp.Application.Dtos;

namespace SC_LaborReporting.ProductSeries;

public class ProductSeriesDto : AuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
}

public class CreateUpdateProductSeriesDto
{
    public string Code { get; set; }
    public string Name { get; set; }
}