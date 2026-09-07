using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class SiesaRepository : ISiesaRepository
    {
        private readonly string _connectionString;

        public SiesaRepository(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("SiesaDatabase")
                ?? throw new InvalidOperationException(
                    "No se encontró la cadena de conexión 'SiesaDatabase'.");
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
    }
}