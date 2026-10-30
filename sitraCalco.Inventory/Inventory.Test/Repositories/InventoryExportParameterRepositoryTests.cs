using System.Data.Common;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Inventory.Infrastructure.Persistance.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Test.Repositories;

public class InventoryExportParameterRepositoryTests
{
    // Valores de prueba del contrato SQL; no son semillas ni constantes del repositorio.
    private static readonly Dictionary<string, (int Length, string Value)> TechnicalFields = new()
    {
        ["detail_record_type"] = (4, "0412"),
        ["detail_record_subtype"] = (2, "00"),
        ["detail_record_version"] = (2, "02"),
        ["company_code"] = (10, "009"),
        ["reserved_field"] = (100, new string(' ', 55)),
        ["auxiliary_location"] = (50, new string(' ', 10)),
        ["lot_code"] = (50, new string(' ', 15)),
        ["label_number"] = (50, "000000000"),
        ["count_1_1"] = (20, "00000000000.0000"),
        ["count_2_1"] = (20, "00000000000.0000"),
        ["count_1_2"] = (20, "00000000000.0000"),
        ["count_2_2"] = (20, "00000000000.0000"),
        ["count_3_2"] = (20, "00000000000.0000"),
        ["barcode"] = (50, new string(' ', 20)),
        ["detail_extension_1"] = (50, new string(' ', 20)),
        ["detail_extension_2"] = (50, new string(' ', 20)),
        ["header_record_type"] = (4, "0000"),
        ["header_record_subtype"] = (2, "00"),
        ["header_record_version"] = (2, "01"),
        ["footer_record_type"] = (4, "9999"),
        ["footer_record_subtype"] = (2, "00"),
        ["footer_record_version"] = (2, "01")
    };

    [Fact]
    public void MySqlMapping_MatchesExistingSchema_WithoutOpeningAConnection()
    {
        // La versión explícita evita AutoDetect. Solo se construyen metadatos en memoria.
        using var context = new SitraCalcoContext(new DbContextOptionsBuilder<SitraCalcoContext>()
            .UseMySql("Server=unused;Database=unused;User ID=unused;", new MySqlServerVersion(new Version(8, 0, 36)))
            .Options);
        var entity = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(InventoryExportParameter))!;
        Assert.Equal("sitracalco_inventory_export_parameter", entity.GetTableName());
        Assert.Equal(28, entity.GetProperties().Count());
        Assert.Equal(22, TechnicalFields.Count);
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
        foreach (var property in entity.GetProperties())
            Assert.Equal(property.Name, property.GetColumnName(table));

        var strings = TechnicalFields.ToDictionary(field => field.Key, field => field.Value.Length);
        strings.Add("configuration_code", 100);
        strings.Add("configuration_name", 200);
        foreach (var (name, length) in strings)
        {
            var property = entity.FindProperty(name)!;
            Assert.Equal(typeof(string), property.ClrType);
            Assert.Equal($"varchar({length})", property.GetColumnType());
            Assert.Equal(length, property.GetMaxLength());
            Assert.False(property.IsNullable);
            Assert.Null(property.GetValueConverter());
        }

        var key = entity.FindPrimaryKey()!;
        Assert.Equal("PRIMARY", key.GetName());
        Assert.Equal("export_parameter_id", Assert.Single(key.Properties).Name);
        var id = entity.FindProperty("export_parameter_id")!;
        Assert.Equal(typeof(long), id.ClrType);
        Assert.Equal("bigint", id.GetColumnType());
        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);
        Assert.Equal("IdentityColumn", id.FindAnnotation("MySql:ValueGenerationStrategy")!.Value!.ToString());
        var index = Assert.Single(entity.GetIndexes());
        Assert.True(index.IsUnique);
        Assert.Equal("uq_export_configuration_code", index.GetDatabaseName());
        Assert.Equal("configuration_code", Assert.Single(index.Properties).Name);

        var active = entity.FindProperty("is_active")!;
        Assert.Equal(typeof(bool), active.ClrType);
        Assert.Equal("tinyint(1)", active.GetColumnType());
        Assert.Equal(true, active.GetDefaultValue());
        var created = entity.FindProperty("created_at")!;
        Assert.Equal("datetime(6)", created.GetColumnType());
        Assert.Equal("CURRENT_TIMESTAMP(6)", created.GetDefaultValueSql());
        Assert.Equal(ValueGenerated.OnAdd, created.ValueGenerated);
        var updated = entity.FindProperty("updated_at")!;
        Assert.Equal(typeof(DateTime?), updated.ClrType);
        Assert.Equal("datetime(6)", updated.GetColumnType());
        Assert.Equal(ValueGenerated.Never, updated.ValueGenerated);
        Assert.Equal("updated_at", Assert.Single(entity.GetProperties(), property => property.IsNullable).Name);
        Assert.Null(updated.GetDefaultValueSql());
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActiveConfiguration_ReadsAll22TechnicalFieldsExactly_WithoutTracking(bool differentStoredValues)
    {
        var row = Row("SIESA_PDV_0412", true);
        if (differentStoredValues)
        {
            // Demuestra que se devuelven los datos leídos, no valores predefinidos en C#.
            row["company_code"] = "007";
            row["reserved_field"] = "  reservado  ";
            row["count_2_1"] = "00000000001.2300";
            row["updated_at"] = new DateTime(2026, 10, 30, 12, 15, 20).AddTicks(1234560);
        }
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var source = new SelectOnlyRows(row, Row("OTRO_CODIGO_DE_PRUEBA", true));
        await using var context = Context(connection, source);
        var repository = new InventoryExportParameterRepository(context);

        var result = await repository.GetActiveByConfigurationCode("SIESA_PDV_0412");

        Assert.NotNull(result);
        foreach (var (name, expected) in row)
            Assert.Equal(expected, typeof(InventoryExportParameter).GetProperty(name)!.GetValue(result));
        Assert.Equal("0412", result.detail_record_type);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(1, source.Reads);
    }

    [Theory]
    [InlineData("INEXISTENTE", true)]
    [InlineData("SIESA_PDV_0412", false)]
    [InlineData(" SIESA_PDV_0412", true)]
    public async Task MissingInactiveOrUnmodifiedLookup_ReturnsNull(string requestedCode, bool active)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var source = new SelectOnlyRows(Row("SIESA_PDV_0412", active), Row("OTRO_CODIGO_DE_PRUEBA", true));
        await using var context = Context(connection, source);

        var result = await new InventoryExportParameterRepository(context).GetActiveByConfigurationCode(requestedCode);

        Assert.Null(result);
        Assert.Equal(1, source.Reads);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public void Persistence_RegistersScopedReadRepository_WithoutResolvingDatabaseOptions()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SitraCalcoDatabase"] = "Server=unused;Database=unused;User ID=unused;"
        }).Build();
        var services = new ServiceCollection();
        services.AddPersistence(configuration);

        var registration = Assert.Single(services, item => item.ServiceType == typeof(IInventoryExportParameterRepository));
        Assert.Equal(typeof(InventoryExportParameterRepository), registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }

    private static Dictionary<string, object?> Row(string code, bool active)
    {
        var row = TechnicalFields.ToDictionary(field => field.Key, field => (object?)field.Value.Value);
        row["export_parameter_id"] = 7L;
        row["configuration_code"] = code;
        row["configuration_name"] = " Parámetros de prueba con espacios ";
        row["is_active"] = active;
        row["created_at"] = new DateTime(2026, 10, 30, 10, 20, 30).AddTicks(6543210);
        row["updated_at"] = null;
        return row;
    }

    private static SitraCalcoContext Context(SqliteConnection connection, SelectOnlyRows source) => new(
        new DbContextOptionsBuilder<SitraCalcoContext>().UseSqlite(connection).AddInterceptors(source).Options);

    // La consulta EF real se ejecuta contra una CTE de solo lectura con parámetros.
    // No hay tablas, EnsureCreated, migraciones, INSERT ni conexión a MySQL.
    private sealed class SelectOnlyRows(params Dictionary<string, object?>[] rows) : DbCommandInterceptor
    {
        public int Reads { get; private set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Assert.StartsWith("SELECT", command.CommandText);
            Assert.Contains("sitracalco_inventory_export_parameter", command.CommandText);
            Reads++;
            var selects = new List<string>();
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var columns = new List<string>();
                foreach (var (name, value) in rows[rowIndex])
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@fixture_{rowIndex}_{columns.Count}";
                    parameter.Value = value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                    columns.Add($"{parameter.ParameterName} AS \"{name}\"");
                }
                selects.Add("SELECT " + string.Join(", ", columns));
            }
            command.CommandText = "WITH \"sitracalco_inventory_export_parameter\" AS (" +
                string.Join(" UNION ALL ", selects) + ") " + command.CommandText;
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Estas pruebas no admiten comandos de escritura.");
    }
}
