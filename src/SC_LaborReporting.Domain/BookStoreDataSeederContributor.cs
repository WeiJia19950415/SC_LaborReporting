using SC_LaborReporting.Books;
using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.PermissionManagement;

namespace SC_LaborReporting;

public class SC_LaborReportingDataSeederContributor
    : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<Book, Guid> _bookRepository;
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;
    private readonly IPermissionDataSeeder _permissionDataSeeder;

    public SC_LaborReportingDataSeederContributor(IRepository<Book, Guid> bookRepository,
        IPermissionDefinitionManager permissionDefinitionManager,
            IPermissionDataSeeder permissionDataSeeder)
    {
        _bookRepository = bookRepository;
        _permissionDefinitionManager = permissionDefinitionManager;
        _permissionDataSeeder = permissionDataSeeder;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        // 1. 从系统中获取所有已定义的权限（包括 ABP 自带的和你自定义的）
        var permissionNames = (await _permissionDefinitionManager.GetPermissionsAsync())
            .Select(p => p.Name)
            .ToArray();

        // 2. 将这些权限全部赋予 "admin" 角色
        await _permissionDataSeeder.SeedAsync(
            RolePermissionValueProvider.ProviderName,
            "admin",
            permissionNames,
            context?.TenantId);
        if (await _bookRepository.GetCountAsync() <= 0)
        {
            await _bookRepository.InsertAsync(
                new Book
                {
                    Name = "1984",
                    Type = BookType.Dystopia,
                    PublishDate = new DateTime(1949, 6, 8),
                    Price = 19.84f
                },
                autoSave: true
            );

            await _bookRepository.InsertAsync(
                new Book
                {
                    Name = "The Hitchhiker's Guide to the Galaxy",
                    Type = BookType.ScienceFiction,
                    PublishDate = new DateTime(1995, 9, 27),
                    Price = 42.0f
                },
                autoSave: true
            );
        }
    }
}