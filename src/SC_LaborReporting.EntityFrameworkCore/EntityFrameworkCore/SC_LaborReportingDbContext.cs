using Microsoft.EntityFrameworkCore;
using SC_LaborReporting.Books;
using SC_LaborReporting.LaborCategories;
using SC_LaborReporting.LaborReports;
using SC_LaborReporting.Projects;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace SC_LaborReporting.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class SC_LaborReportingDbContext :
    AbpDbContext<SC_LaborReportingDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    public DbSet<Book> Books { get; set; }

    #region Entities from the modules
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    public DbSet<LaborCategory> LaborCategories { get; set; }
    public DbSet<LaborCategoryDepartment> LaborCategoryDepartments { get; set; }
    public DbSet<LaborCategoryRole> LaborCategoryRoles { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<LaborReport> LaborReports { get; set; }
    public DbSet<LaborReportDetail> LaborReportDetails { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }
    #endregion

    public SC_LaborReportingDbContext(DbContextOptions<SC_LaborReportingDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();
        
        builder.Entity<Book>(b =>
        {
            b.ToTable(SC_LaborReportingConsts.DbTablePrefix + "Books",
                SC_LaborReportingConsts.DbSchema);
            b.ConfigureByConvention(); //auto configure for the base class props
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        });

        builder.Entity<LaborCategory>(b =>
        {
            b.ToTable(SC_LaborReportingConsts.DbTablePrefix + "LaborCategories", SC_LaborReportingConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasMany(x => x.Departments).WithOne().HasForeignKey(x => x.LaborCategoryId).IsRequired();
            b.HasMany(x => x.Roles).WithOne().HasForeignKey(x => x.LaborCategoryId).IsRequired();
        });

        builder.Entity<Project>(b =>
        {
            b.ToTable(SC_LaborReportingConsts.DbTablePrefix + "Projects", SC_LaborReportingConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Code).IsRequired().HasMaxLength(64);
        });

        builder.Entity<LaborReport>(b =>
        {
            b.ToTable(SC_LaborReportingConsts.DbTablePrefix + "LaborReports", SC_LaborReportingConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasMany(x => x.Details)
             .WithOne(x => x.LaborReport)
             .HasForeignKey(x => x.LaborReportId)
             .IsRequired();
        });

        builder.Entity<LaborReportDetail>(b =>
        {
            b.ToTable(SC_LaborReportingConsts.DbTablePrefix + "LaborReportDetails", SC_LaborReportingConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.LaborCategoryCode).IsRequired().HasMaxLength(64);
            b.Property(x => x.Hours).HasColumnType("decimal(18,2)");
            b.Property(x => x.Jobresponsibilities).HasMaxLength(1024);
        });
    }
}
