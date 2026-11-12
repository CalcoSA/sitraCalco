using Microsoft.Extensions.Configuration;
using Inventory.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Inventory.Domain.Models;
using Inventory.Domain.Dtos;
using System.Data;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class SiesaRepository : ISiesaRepository
    {
        private readonly string _connectionString;

        public SiesaRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("SiesaDatabase") ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'SiesaDatabase'.");
        }

        public async Task<IEnumerable<Product>> GetProducts()
        {
            const string query = @"
                SELECT
                    i.f120_id_cia,
                    i.f120_rowid,
                    i.f120_referencia,
                    i.f120_descripcion,
                    u.f122_id_unidad,
                    um.f101_descripcion AS nombre_unidad,
                    u.f122_factor,
                    u.f122_peso,
                    c.f125_id_plan,
                    c.f125_id_criterio_mayor,
                    cm.f106_descripcion,
                    cm.f106_notas
                FROM dbo.t120_mc_items AS i
                LEFT JOIN dbo.t122_mc_items_unidades AS u
                    ON i.f120_rowid = u.f122_rowid_item
                    AND i.f120_id_cia = u.f122_id_cia
                LEFT JOIN dbo.t101_mc_unidades_medida AS um
                    ON u.f122_id_unidad = um.f101_id
                    AND u.f122_id_cia = um.f101_id_cia
                INNER JOIN dbo.t125_mc_items_criterios AS c
                    ON i.f120_rowid = c.f125_rowid_item
                    AND i.f120_id_cia = c.f125_id_cia
                LEFT JOIN dbo.t106_mc_criterios_item_mayores AS cm
                    ON c.f125_id_plan = cm.f106_id_plan
                    AND c.f125_id_criterio_mayor = cm.f106_id
                    AND c.f125_id_cia = cm.f106_id_cia
                WHERE
                    i.f120_id_cia = '9'
                    AND c.f125_id_plan IN ('FAC', 'FIF', 'ECL')
                    AND (
                        c.f125_id_plan <> 'ECL'
                        OR cm.f106_descripcion NOT IN (
                            'Compras de Servicios',
                            'Papeleria y Servicio al Cliente'
                        )
                    )
                ORDER BY
                    i.f120_referencia,
                    c.f125_id_plan,
                    u.f122_id_unidad;
            ";

            var products = new List<Product>();

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(query, connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (reader["nombre_unidad"] == DBNull.Value)
                    continue;

                var product = new Product
                {
                    product_name =
                        Convert.ToString(
                            reader["f120_descripcion"]
                        ) ?? string.Empty,

                    reference =
                        Convert.ToString(
                            reader["f120_referencia"]
                        ) ?? string.Empty,

                    unit_of_measure =
                        Convert.ToString(
                            reader["nombre_unidad"]
                        )?.Trim() ?? string.Empty,

                    f122_factor =
                        reader["f122_factor"] == DBNull.Value
                            ? null
                            : Convert.ToDecimal(reader["f122_factor"]),

                    plan_id =
                        reader["f125_id_plan"] == DBNull.Value
                            ? null
                            : Convert.ToString(
                                reader["f125_id_plan"]
                            ),

                    image_path = null
                };

                products.Add(product);
            }

            return products;
        }

        public async Task<IReadOnlyDictionary<string, SiesaExistenceDto>> GetExistences(string warehouseCode, IReadOnlyCollection<string> references)
        {
            if (string.IsNullOrWhiteSpace(warehouseCode))
                throw new ArgumentException("El código de bodega es obligatorio.", nameof(warehouseCode));

            ArgumentNullException.ThrowIfNull(references);

            var referenceList = references
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var result = new Dictionary<string, SiesaExistenceDto>(StringComparer.OrdinalIgnoreCase);

            if (referenceList.Length == 0)
                return result;

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var batch in referenceList.Chunk(400))
            {
                var parameterNames = batch
                    .Select((_, index) => $"@ref{index}")
                    .ToArray();

                var query = $@"
                    SELECT
                        RTRIM(i.f120_referencia) AS Referencia,
                        RTRIM(i.f120_id_unidad_inventario) AS UnidadInventario,
                        SUM(x.f400_cant_existencia_1) AS Existencia1,
                        SUM(x.f400_cant_existencia_2) AS Existencia2

                    FROM dbo.t400_cm_existencia AS x

                    INNER JOIN dbo.t150_mc_bodegas AS b
                        ON b.f150_rowid = x.f400_rowid_bodega
                        AND b.f150_id_cia = x.f400_id_cia

                    INNER JOIN dbo.t121_mc_items_extensiones AS e
                        ON e.f121_rowid = x.f400_rowid_item_ext
                        AND e.f121_id_cia = x.f400_id_cia

                    INNER JOIN dbo.t120_mc_items AS i
                        ON i.f120_rowid = e.f121_rowid_item
                        AND i.f120_id_cia = x.f400_id_cia

                    WHERE x.f400_id_cia = @companyId
                      AND b.f150_id = @warehouseCode
                      AND i.f120_referencia IN (
                          {string.Join(", ", parameterNames)}
                      )

                    GROUP BY
                        i.f120_rowid,
                        RTRIM(i.f120_referencia),
                        RTRIM(i.f120_id_unidad_inventario);
                ";

                await using var command = new SqlCommand(query, connection);

                command.Parameters.Add("@companyId", SqlDbType.SmallInt)
                    .Value = (short)9;

                command.Parameters.Add("@warehouseCode", SqlDbType.VarChar, 10)
                    .Value = warehouseCode.Trim();

                for (var index = 0; index < batch.Length; index++)
                {
                    command.Parameters.Add(
                        parameterNames[index],
                        SqlDbType.VarChar,
                        100
                    ).Value = batch[index];
                }

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var existencia = new SiesaExistenceDto
                    {
                        Reference = Convert.ToString(
                            reader["Referencia"])?.Trim() ?? string.Empty,

                        UnitInventory = Convert.ToString(
                            reader["UnidadInventario"])?.Trim() ?? string.Empty,

                        Existence1 = reader["Existencia1"] == DBNull.Value
                            ? null
                            : Convert.ToDecimal(reader["Existencia1"]),

                        Existence2 = reader["Existencia2"] == DBNull.Value
                            ? null
                            : Convert.ToDecimal(reader["Existencia2"])
                    };

                    if (!result.TryAdd(existencia.Reference, existencia))
                    {
                        throw new InvalidOperationException(
                            $"Siesa devolvió varios registros consolidados " +
                            $"para la referencia {existencia.Reference} " +
                            $"en la bodega {warehouseCode}. " +
                            "Verifique los artículos y unidades de inventario.");
                    }
                }
            }

            return result;
        }
    }
}