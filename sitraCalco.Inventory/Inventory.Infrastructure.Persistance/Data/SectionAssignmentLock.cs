using Inventory.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Data;

internal static class SectionAssignmentLock
{
    // Todos los escritores de una pareja deben tomar este bloqueo antes de leer
    // sus dependencias. InnoDB lo mantiene hasta COMMIT/ROLLBACK, también entre instancias.
    internal static async Task<SolutionCenterSection?> LockSectionAssignment(
        this SitraCalcoContext context, long solutionCenterId, long sectionId)
    {
        if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El bloqueo de la sección requiere una transacción.");

        if (context.Database.IsMySql())
        {
            // ToList evita componer una subconsulta sobre FOR UPDATE. AsNoTracking
            // evita devolver el estado anterior que Application pudiera haber leído.
            var rows = await context.SolutionCenterSections.FromSqlInterpolated($"""
                SELECT solution_center_section_id, solution_center_id, section_id, is_active
                FROM sitracalco_inventory_solution_center_section
                WHERE solution_center_id = {solutionCenterId} AND section_id = {sectionId}
                FOR UPDATE
                """).AsNoTracking().ToListAsync();
            return rows.SingleOrDefault();
        }

        // SQLite usa la transacción de escritura inmediata en las pruebas aisladas.
        if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
            throw new NotSupportedException("El proveedor no admite el bloqueo de secciones configurado.");

        return await context.SolutionCenterSections.AsNoTracking().SingleOrDefaultAsync(
            assignment => assignment.solution_center_id == solutionCenterId && assignment.section_id == sectionId);
    }
}
