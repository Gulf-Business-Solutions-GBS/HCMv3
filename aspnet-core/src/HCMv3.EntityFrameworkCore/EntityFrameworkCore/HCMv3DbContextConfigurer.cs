using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.EntityFrameworkCore;

public static class HCMv3DbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<HCMv3DbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<HCMv3DbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}

