using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SC_LaborReporting.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class SC_LaborReportingDbContextFactory : IDesignTimeDbContextFactory<SC_LaborReportingDbContext>
{
    public SC_LaborReportingDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        SC_LaborReportingEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<SC_LaborReportingDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new SC_LaborReportingDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../SC_LaborReporting.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
