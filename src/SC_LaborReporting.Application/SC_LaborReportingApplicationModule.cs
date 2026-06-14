using Microsoft.Extensions.DependencyInjection;
using SC_LaborReporting.Permissions;
using Volo.Abp.Account;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Mapperly;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace SC_LaborReporting;

[DependsOn(
    typeof(SC_LaborReportingDomainModule),
    typeof(SC_LaborReportingApplicationContractsModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class SC_LaborReportingApplicationModule : AbpModule
{

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        //Configure<AbpPermissionOptions>(options =>
        //{
        //    options.DefinitionProviders.Add<SC_LaborReportingPermissionDefinitionProvider>();
        //});
    }
}
