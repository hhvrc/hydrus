using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace HydrusReadModel;

/// <summary>
/// Used only by `dotnet ef migrations` / `dotnet ef dbcontext script`. Never
/// connects at design time -- the connection string is a placeholder, overridden
/// by HYDRUS_PG_CONNECTION when actually applying migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HydrusDbContext>
{
    public HydrusDbContext CreateDbContext(string[] args)
    {
        var connString = Environment.GetEnvironmentVariable("HYDRUS_PG_CONNECTION")
            ?? "Host=localhost;Database=hydrus;Username=hydrus";

        var dsb = new NpgsqlDataSourceBuilder(connString);
        dsb.MapEnum<ContentStatus>("hydrus.content_status");
        dsb.MapEnum<Filetype>("hydrus.filetype");
        dsb.MapEnum<ServiceType>("hydrus.service_type");
        dsb.MapEnum<SyncKind>("hydrus.sync_kind");
        dsb.MapEnum<DiscrepancyKind>("hydrus.discrepancy_kind");

        var options = new DbContextOptionsBuilder<HydrusDbContext>()
            .UseNpgsql(dsb.Build(), HydrusDbContext.ConfigureEnums)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new HydrusDbContext(options);
    }
}
