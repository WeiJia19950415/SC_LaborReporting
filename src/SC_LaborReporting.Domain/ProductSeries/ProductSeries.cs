using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace SC_LaborReporting.ProductSeries;

/// <summary>
/// 产品系列实体
/// </summary>
public class ProductSeries : FullAuditedEntity<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }

    public ProductSeries() { }

    public ProductSeries(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
    }
}