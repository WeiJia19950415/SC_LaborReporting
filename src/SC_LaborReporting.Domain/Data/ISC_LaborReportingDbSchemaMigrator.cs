using System.Threading.Tasks;

namespace SC_LaborReporting.Data;

public interface ISC_LaborReportingDbSchemaMigrator
{
    Task MigrateAsync();
}
