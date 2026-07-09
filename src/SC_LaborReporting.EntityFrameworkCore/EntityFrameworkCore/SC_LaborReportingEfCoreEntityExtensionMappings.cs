using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.ObjectExtending;
using Volo.Abp.Threading;

namespace SC_LaborReporting.EntityFrameworkCore;

public static class SC_LaborReportingEfCoreEntityExtensionMappings
{
    private static readonly OneTimeRunner OneTimeRunner = new OneTimeRunner();

    public static void Configure()
    {
        SC_LaborReportingGlobalFeatureConfigurator.Configure();
        SC_LaborReportingModuleExtensionConfigurator.Configure();

        OneTimeRunner.Run(() =>
        {
            ObjectExtensionManager.Instance
            .MapEfCoreProperty<IdentityUser, string>(
                "JobNumber",
                (entityBuilder, propertyBuilder) =>
                {
                    propertyBuilder.HasMaxLength(32);
                    entityBuilder.HasIndex("JobNumber").IsUnique();
                }
            );
        });
    }
}
