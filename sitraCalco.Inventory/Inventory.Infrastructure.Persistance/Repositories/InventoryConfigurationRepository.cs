using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class InventoryConfigurationRepository
        : IInventoryConfigurationRepository
    {
        private readonly SitraCalcoContext _context;

        public InventoryConfigurationRepository(
            SitraCalcoContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Valida si ya existe una configuración
        /// de inventario con el mismo nombre.
        /// </summary>
        public async Task<bool> ExistsByName(
            string inventoryConfigurationName)
        {
            var normalizedName =
                inventoryConfigurationName
                    .Trim()
                    .ToUpper();

            return await _context
                .InventoryConfigurations
                .AsNoTracking()
                .AnyAsync(configuration =>
                    configuration
                        .inventory_configuration_name
                        .ToUpper() ==
                    normalizedName);
        }

        /// <summary>
        /// Crea una configuración de inventario
        /// y retorna su identificador generado.
        /// </summary>
        public async Task<long> CreateInventoryConfiguration(
            InventoryConfiguration inventoryConfiguration)
        {
            await _context
                .InventoryConfigurations
                .AddAsync(inventoryConfiguration);

            await _context.SaveChangesAsync();

            return inventoryConfiguration
                .inventory_configuration_id;
        }

        /// <summary>
        /// Valida si existe una configuración de inventario.
        /// </summary>
        public async Task<bool> ConfigurationExists(
            long inventoryConfigurationId)
        {
            return await _context.InventoryConfigurations
                .AsNoTracking()
                .AnyAsync(configuration =>
                    configuration.inventory_configuration_id ==
                    inventoryConfigurationId);
        }

        /// <summary>
        /// Valida si existe una bodega o punto de venta.
        /// </summary>
        public async Task<bool> SolutionCenterExists(
            long solutionCenterId)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(solutionCenter =>
                    solutionCenter.solution_center_id ==
                    solutionCenterId);
        }

        /// <summary>
        /// Valida si existe una sección.
        /// </summary>
        public async Task<bool> SectionExists(
            long sectionId)
        {
            return await _context.Sections
                .AsNoTracking()
                .AnyAsync(section =>
                    section.section_id ==
                    sectionId);
        }

        /// <summary>
        /// Valida que una sección esté asociada
        /// a la bodega o punto de venta indicado.
        /// </summary>
        public async Task<bool> SectionBelongsToSolutionCenter(
            long solutionCenterId,
            long sectionId)
        {
            return await _context.SolutionCenterProducts
                .AsNoTracking()
                .AnyAsync(item =>
                    item.solution_center_id ==
                    solutionCenterId
                    &&
                    item.section_id ==
                    sectionId);
        }

        /// <summary>
        /// Valida si ya existe exactamente la misma
        /// combinación configuración + centro + sección.
        /// </summary>
        public async Task<bool> AssignmentExists(
            long inventoryConfigurationId,
            long solutionCenterId,
            long sectionId)
        {
            return await _context
                .InventoryConfigurationAssignments
                .AsNoTracking()
                .AnyAsync(assignment =>
                    assignment.inventory_configuration_id ==
                    inventoryConfigurationId
                    &&
                    assignment.solution_center_id ==
                    solutionCenterId
                    &&
                    assignment.section_id ==
                    sectionId);
        }

        /// <summary>
        /// Guarda las asociaciones de una configuración
        /// de inventario en una sola operación.
        /// </summary>
        public async Task<int> CreateAssignments(
            IEnumerable<InventoryConfigurationAssignment> assignments)
        {
            var assignmentsToCreate =
                assignments.ToList();

            if (assignmentsToCreate.Count == 0)
                return 0;

            await _context
                .InventoryConfigurationAssignments
                .AddRangeAsync(assignmentsToCreate);

            await _context.SaveChangesAsync();

            return assignmentsToCreate.Count;
        }

        /// <summary>
        /// Obtiene todos los centros de soluciones de un tipo
        /// sin paginación.
        /// Tipo 1: Bodega.
        /// Tipo 2: Punto de Venta.
        /// </summary>
        public async Task<IEnumerable<SolutionCenterListDto>>
            GetSolutionCentersByType(
                long solutionCenterTypeId)
        {
            return await (
                from solutionCenter in
                    _context.SolutionCenters.AsNoTracking()

                join solutionCenterType in
                    _context.SolutionCenterTypes.AsNoTracking()

                on solutionCenter.solution_center_type_id
                    equals solutionCenterType.solution_center_type_id

                where solutionCenter.solution_center_type_id ==
                      solutionCenterTypeId

                orderby solutionCenter.solution_center_name

                select new SolutionCenterListDto
                {
                    SolutionCenterId =
                        solutionCenter.solution_center_id,

                    SolutionCenterTypeId =
                        solutionCenter.solution_center_type_id,

                    SolutionCenterTypeName =
                        solutionCenterType.solution_center_type_name,

                    SolutionCenterCode =
                        solutionCenter.solution_center_code,

                    SolutionCenterName =
                        solutionCenter.solution_center_name,

                    IsActive =
                        solutionCenter.is_active
                }
            ).ToListAsync();
        }

        /// <summary>
        /// Obtiene las secciones asociadas a Puntos de Venta
        /// que no están asociadas a ninguna Bodega.
        /// </summary>
        public async Task<IEnumerable<SectionDto>>
            GetPointOfSaleOnlySections()
        {
            const long warehouseTypeId = 1;
            const long pointOfSaleTypeId = 2;

            var warehouseSectionIds =
                from solutionCenterProduct in
                    _context.SolutionCenterProducts.AsNoTracking()

                join solutionCenter in
                    _context.SolutionCenters.AsNoTracking()

                on solutionCenterProduct.solution_center_id
                    equals solutionCenter.solution_center_id

                where solutionCenter.solution_center_type_id ==
                      warehouseTypeId

                select solutionCenterProduct.section_id;

            var pointOfSaleSectionIds =
                from solutionCenterProduct in
                    _context.SolutionCenterProducts.AsNoTracking()

                join solutionCenter in
                    _context.SolutionCenters.AsNoTracking()

                on solutionCenterProduct.solution_center_id
                    equals solutionCenter.solution_center_id

                where solutionCenter.solution_center_type_id ==
                      pointOfSaleTypeId

                select solutionCenterProduct.section_id;

            return await _context.Sections
                .AsNoTracking()
                .Where(section =>
                    pointOfSaleSectionIds.Contains(
                        section.section_id)
                    &&
                    !warehouseSectionIds.Contains(
                        section.section_id))
                .OrderBy(section =>
                    section.section_name)
                .Select(section =>
                    new SectionDto
                    {
                        SectionId =
                            section.section_id,

                        SectionName =
                            section.section_name,

                        IsActive =
                            section.is_active
                    })
                .ToListAsync();
        }
        /// <summary>
        /// Obtiene la información de una bodega o punto de venta
        /// junto con todas sus secciones asociadas.
        /// </summary>
        public async Task<SolutionCenterSectionsDto?>
            GetSolutionCenterWithSections(
                long solutionCenterId)
        {
            var solutionCenter =
                await (
                    from center in
                        _context.SolutionCenters.AsNoTracking()

                    join type in
                        _context.SolutionCenterTypes.AsNoTracking()

                    on center.solution_center_type_id
                        equals type.solution_center_type_id

                    where center.solution_center_id ==
                          solutionCenterId

                    select new SolutionCenterSectionsDto
                    {
                        SolutionCenterId =
                            center.solution_center_id,

                        SolutionCenterTypeId =
                            center.solution_center_type_id,

                        SolutionCenterTypeName =
                            type.solution_center_type_name,

                        SolutionCenterCode =
                            center.solution_center_code,

                        SolutionCenterName =
                            center.solution_center_name,

                        IsActive =
                            center.is_active
                    }
                ).FirstOrDefaultAsync();

            if (solutionCenter is null)
                return null;

            var sectionIds =
                await _context.SolutionCenterProducts
                    .AsNoTracking()
                    .Where(item =>
                        item.solution_center_id ==
                        solutionCenterId)
                    .Select(item =>
                        item.section_id)
                    .Distinct()
                    .ToListAsync();

            solutionCenter.Sections =
                await _context.Sections
                    .AsNoTracking()
                    .Where(section =>
                        sectionIds.Contains(
                            section.section_id))
                    .OrderBy(section =>
                        section.section_name)
                    .Select(section =>
                        new SectionDto
                        {
                            SectionId =
                                section.section_id,

                            SectionName =
                                section.section_name,

                            IsActive =
                                section.is_active
                        })
                    .ToListAsync();

            return solutionCenter;
        }
        /// <summary>
        /// Obtiene una sección junto con todos los
        /// Puntos de Venta asociados a ella.
        /// </summary>
        public async Task<SectionSolutionCentersDto?>
            GetSectionWithPointOfSales(
                long sectionId)
        {
            const long pointOfSaleTypeId = 2;

            var section =
                await _context.Sections
                    .AsNoTracking()
                    .Where(section =>
                        section.section_id == sectionId)
                    .Select(section =>
                        new SectionSolutionCentersDto
                        {
                            SectionId =
                                section.section_id,

                            SectionName =
                                section.section_name,

                            IsActive =
                                section.is_active
                        })
                    .FirstOrDefaultAsync();

            if (section is null)
                return null;

            var solutionCenterIds =
                await _context.SolutionCenterProducts
                    .AsNoTracking()
                    .Where(item =>
                        item.section_id == sectionId)
                    .Select(item =>
                        item.solution_center_id)
                    .Distinct()
                    .ToListAsync();

            section.SolutionCenters =
                await (
                    from solutionCenter in
                        _context.SolutionCenters.AsNoTracking()

                    join solutionCenterType in
                        _context.SolutionCenterTypes.AsNoTracking()

                    on solutionCenter.solution_center_type_id
                        equals solutionCenterType.solution_center_type_id

                    where solutionCenterIds.Contains(
                            solutionCenter.solution_center_id)
                        &&
                        solutionCenter.solution_center_type_id ==
                            pointOfSaleTypeId

                    orderby solutionCenter.solution_center_name

                    select new SolutionCenterListDto
                    {
                        SolutionCenterId =
                            solutionCenter.solution_center_id,

                        SolutionCenterTypeId =
                            solutionCenter.solution_center_type_id,

                        SolutionCenterTypeName =
                            solutionCenterType.solution_center_type_name,

                        SolutionCenterCode =
                            solutionCenter.solution_center_code,

                        SolutionCenterName =
                            solutionCenter.solution_center_name,

                        IsActive =
                            solutionCenter.is_active
                    }
                ).ToListAsync();

            return section;
        }
        /// <summary>
        /// Obtiene una bodega o punto de venta junto con
        /// todas las configuraciones de inventario que tiene asociadas,
        /// incluyendo días y secciones.
        /// </summary>
        public async Task<SolutionCenterInventoryConfigurationsDto?>
            GetInventoryConfigurationsBySolutionCenterId(
                long solutionCenterId)
        {
            // 1. Obtener información de la bodega o punto de venta.
            var solutionCenter =
                await (
                    from center in
                        _context.SolutionCenters.AsNoTracking()

                    join type in
                        _context.SolutionCenterTypes.AsNoTracking()

                    on center.solution_center_type_id
                        equals type.solution_center_type_id

                    where center.solution_center_id ==
                          solutionCenterId

                    select new SolutionCenterInventoryConfigurationsDto
                    {
                        SolutionCenterId =
                            center.solution_center_id,

                        SolutionCenterTypeId =
                            center.solution_center_type_id,

                        SolutionCenterTypeName =
                            type.solution_center_type_name,

                        SolutionCenterCode =
                            center.solution_center_code,

                        SolutionCenterName =
                            center.solution_center_name,

                        IsActive =
                            center.is_active
                    }
                ).FirstOrDefaultAsync();

            if (solutionCenter is null)
                return null;

            // 2. Obtener las configuraciones y secciones
            // asociadas a este Solution Center.
            var assignmentRows =
                await (
                    from assignment in
                        _context.InventoryConfigurationAssignments
                            .AsNoTracking()

                    join configuration in
                        _context.InventoryConfigurations
                            .AsNoTracking()

                    on assignment.inventory_configuration_id
                        equals configuration.inventory_configuration_id

                    join section in
                        _context.Sections.AsNoTracking()

                    on assignment.section_id
                        equals section.section_id

                    where assignment.solution_center_id ==
                          solutionCenterId

                    orderby
                        configuration.inventory_configuration_name,
                        section.section_name

                    select new
                    {
                        InventoryConfigurationId =
                            configuration.inventory_configuration_id,

                        InventoryConfigurationName =
                            configuration.inventory_configuration_name,

                        StartDate =
                            configuration.start_date,

                        EndDate =
                            configuration.end_date,

                        SectionId =
                            section.section_id,

                        SectionName =
                            section.section_name,

                        SectionIsActive =
                            section.is_active,

                        AssignmentIsActive =
                            assignment.is_active
                    }
                ).ToListAsync();

            // El centro existe pero todavía no tiene
            // configuraciones de inventario.
            if (assignmentRows.Count == 0)
                return solutionCenter;

            // 3. Obtener únicamente los días de las
            // configuraciones encontradas.
            var configurationIds =
                assignmentRows
                    .Select(item =>
                        item.InventoryConfigurationId)
                    .Distinct()
                    .ToList();

            var dayRows =
                await _context.InventoryConfigurationDays
                    .AsNoTracking()
                    .Where(day =>
                        configurationIds.Contains(
                            day.inventory_configuration_id))
                    .OrderBy(day =>
                        day.inventory_configuration_day_id)
                    .Select(day => new
                    {
                        InventoryConfigurationId =
                            day.inventory_configuration_id,

                        DayOfWeek =
                            day.day_of_week
                    })
                    .ToListAsync();

            // 4. Agrupar las asociaciones por configuración.
            solutionCenter.Configurations =
                assignmentRows
                    .GroupBy(item => new
                    {
                        item.InventoryConfigurationId,
                        item.InventoryConfigurationName,
                        item.StartDate,
                        item.EndDate
                    })
                    .Select(group =>
                        new InventoryConfigurationDetailDto
                        {
                            InventoryConfigurationId =
                                group.Key.InventoryConfigurationId,

                            InventoryConfigurationName =
                                group.Key.InventoryConfigurationName,

                            StartDate =
                                group.Key.StartDate,

                            EndDate =
                                group.Key.EndDate,

                            Days =
                                dayRows
                                    .Where(day =>
                                        day.InventoryConfigurationId ==
                                        group.Key.InventoryConfigurationId)
                                    .Select(day =>
                                        day.DayOfWeek)
                                    .ToList(),

                            Sections =
                                group
                                    .Select(item =>
                                        new InventoryConfigurationSectionDto
                                        {
                                            SectionId =
                                                item.SectionId,

                                            SectionName =
                                                item.SectionName,

                                            SectionIsActive =
                                                item.SectionIsActive,

                                            IsActive =
                                                item.AssignmentIsActive
                                        })
                                    .ToList()
                        })
                    .OrderBy(configuration =>
                        configuration.InventoryConfigurationName)
                    .ToList();

            return solutionCenter;
        }
        /// <summary>
        /// Obtiene una configuración de inventario por su identificador,
        /// incluyendo sus días, bodegas o puntos de venta y secciones.
        /// </summary>
        public async Task<InventoryConfigurationByIdDto?>
            GetInventoryConfigurationById(
                long inventoryConfigurationId)
        {
            // 1. Obtener la configuración principal.
            var configuration =
                await _context.InventoryConfigurations
                    .AsNoTracking()
                    .Where(item =>
                        item.inventory_configuration_id ==
                        inventoryConfigurationId)
                    .Select(item =>
                        new InventoryConfigurationByIdDto
                        {
                            InventoryConfigurationId =
                                item.inventory_configuration_id,

                            InventoryConfigurationName =
                                item.inventory_configuration_name,

                            StartDate =
                                item.start_date,

                            EndDate =
                                item.end_date
                        })
                    .FirstOrDefaultAsync();

            if (configuration is null)
                return null;

            // 2. Obtener los días asociados.
            configuration.Days =
                await _context.InventoryConfigurationDays
                    .AsNoTracking()
                    .Where(day =>
                        day.inventory_configuration_id ==
                        inventoryConfigurationId)
                    .OrderBy(day =>
                        day.inventory_configuration_day_id)
                    .Select(day =>
                        day.day_of_week)
                    .ToListAsync();

            // 3. Obtener las asociaciones de la configuración.
            var assignmentRows =
                await (
                    from assignment in
                        _context.InventoryConfigurationAssignments
                            .AsNoTracking()

                    join solutionCenter in
                        _context.SolutionCenters
                            .AsNoTracking()

                    on assignment.solution_center_id
                        equals solutionCenter.solution_center_id

                    join solutionCenterType in
                        _context.SolutionCenterTypes
                            .AsNoTracking()

                    on solutionCenter.solution_center_type_id
                        equals solutionCenterType.solution_center_type_id

                    join section in
                        _context.Sections
                            .AsNoTracking()

                    on assignment.section_id
                        equals section.section_id

                    where assignment.inventory_configuration_id ==
                          inventoryConfigurationId

                    orderby
                        solutionCenter.solution_center_name,
                        section.section_name

                    select new
                    {
                        SolutionCenterId =
                            solutionCenter.solution_center_id,

                        SolutionCenterTypeId =
                            solutionCenter.solution_center_type_id,

                        SolutionCenterTypeName =
                            solutionCenterType.solution_center_type_name,

                        SolutionCenterCode =
                            solutionCenter.solution_center_code,

                        SolutionCenterName =
                            solutionCenter.solution_center_name,

                        SolutionCenterIsActive =
                            solutionCenter.is_active,

                        SectionId =
                            section.section_id,

                        SectionName =
                            section.section_name,

                        SectionIsActive =
                            section.is_active,

                        AssignmentIsActive =
                            assignment.is_active
                    }
                ).ToListAsync();

            // La configuración puede existir aunque todavía
            // no tenga bodegas o puntos de venta asociados.
            if (assignmentRows.Count == 0)
                return configuration;

            // 4. Agrupar todas las secciones por Solution Center.
            configuration.SolutionCenters =
                assignmentRows
                    .GroupBy(item => new
                    {
                        item.SolutionCenterId,
                        item.SolutionCenterTypeId,
                        item.SolutionCenterTypeName,
                        item.SolutionCenterCode,
                        item.SolutionCenterName,
                        item.SolutionCenterIsActive
                    })
                    .Select(group =>
                        new InventoryConfigurationSolutionCenterDto
                        {
                            SolutionCenterId =
                                group.Key.SolutionCenterId,

                            SolutionCenterTypeId =
                                group.Key.SolutionCenterTypeId,

                            SolutionCenterTypeName =
                                group.Key.SolutionCenterTypeName,

                            SolutionCenterCode =
                                group.Key.SolutionCenterCode,

                            SolutionCenterName =
                                group.Key.SolutionCenterName,

                            IsActive =
                                group.Key.SolutionCenterIsActive,

                            Sections =
                                group
                                    .Select(item =>
                                        new InventoryConfigurationSectionDto
                                        {
                                            SectionId =
                                                item.SectionId,

                                            SectionName =
                                                item.SectionName,

                                            SectionIsActive =
                                                item.SectionIsActive,

                                            IsActive =
                                                item.AssignmentIsActive
                                        })
                                    .OrderBy(section =>
                                        section.SectionName)
                                    .ToList()
                        })
                    .OrderBy(solutionCenter =>
                        solutionCenter.SolutionCenterName)
                    .ToList();

            return configuration;
        }
        /// <summary>
        /// Actualiza el estado de una asociación entre
        /// configuración de inventario, bodega o punto de venta y sección.
        /// </summary>
        public async Task<bool> UpdateAssignmentStatus(
            long inventoryConfigurationId,
            long solutionCenterId,
            long sectionId,
            bool isActive)
        {
            var assignment =
                await _context.InventoryConfigurationAssignments
                    .FirstOrDefaultAsync(item =>
                        item.inventory_configuration_id ==
                            inventoryConfigurationId
                        &&
                        item.solution_center_id ==
                            solutionCenterId
                        &&
                        item.section_id ==
                            sectionId);

            if (assignment is null)
                return false;

            assignment.is_active = isActive;

            await _context.SaveChangesAsync();

            return true;
        }
        /// <summary>
        /// Valida si existe otra configuración de inventario
        /// con el mismo nombre, excluyendo la configuración actual.
        /// </summary>
        public async Task<bool> ExistsByName(
            string inventoryConfigurationName,
            long excludeInventoryConfigurationId)
        {
            var normalizedName =
                inventoryConfigurationName
                    .Trim()
                    .ToUpper();

            return await _context.InventoryConfigurations
                .AsNoTracking()
                .AnyAsync(configuration =>
                    configuration.inventory_configuration_id !=
                        excludeInventoryConfigurationId
                    &&
                    configuration.inventory_configuration_name
                        .ToUpper() == normalizedName);
        }
        /// <summary>
        /// Actualiza el nombre y el rango de fechas
        /// de una configuración de inventario.
        /// </summary>
        public async Task<bool> UpdateInventoryConfiguration(
            long inventoryConfigurationId,
            string inventoryConfigurationName,
            DateTime? startDate,
            DateTime? endDate)
        {
            var configuration =
                await _context.InventoryConfigurations
                    .FirstOrDefaultAsync(item =>
                        item.inventory_configuration_id ==
                        inventoryConfigurationId);

            if (configuration is null)
                return false;

            configuration.inventory_configuration_name =
                inventoryConfigurationName.Trim();

            configuration.start_date =
                startDate?.Date;

            configuration.end_date =
                endDate?.Date;

            await _context.SaveChangesAsync();

            return true;
        }
        /// <summary>
        /// Valida si un día ya está registrado
        /// para una configuración de inventario.
        /// </summary>
        public async Task<bool> DayExists(
            long inventoryConfigurationId,
            string dayOfWeek)
        {
            var normalizedDay =
                dayOfWeek.Trim().ToUpper();

            return await _context.InventoryConfigurationDays
                .AsNoTracking()
                .AnyAsync(day =>
                    day.inventory_configuration_id ==
                        inventoryConfigurationId
                    &&
                    day.day_of_week.ToUpper() ==
                        normalizedDay);
        }
        /// <summary>
        /// Agrega varios días a una configuración
        /// de inventario en una sola operación.
        /// </summary>
        public async Task<int> AddDays(
            IEnumerable<InventoryConfigurationDay> days)
        {
            var daysToCreate =
                days.ToList();

            if (daysToCreate.Count == 0)
                return 0;

            await _context.InventoryConfigurationDays
                .AddRangeAsync(daysToCreate);

            await _context.SaveChangesAsync();

            return daysToCreate.Count;
        }
        /// <summary>
        /// Elimina un día específico de una
        /// configuración de inventario.
        /// </summary>
        public async Task<bool> DeleteDay(
            long inventoryConfigurationId,
            string dayOfWeek)
        {
            var normalizedDay =
                dayOfWeek.Trim().ToUpper();

            var day =
                await _context.InventoryConfigurationDays
                    .FirstOrDefaultAsync(item =>
                        item.inventory_configuration_id ==
                            inventoryConfigurationId
                        &&
                        item.day_of_week.ToUpper() ==
                            normalizedDay);

            if (day is null)
                return false;

            _context.InventoryConfigurationDays
                .Remove(day);

            await _context.SaveChangesAsync();

            return true;
        }
        /// <summary>
        /// Elimina una asociación específica entre
        /// configuración de inventario, Solution Center y sección.
        /// </summary>
        public async Task<bool> DeleteAssignment(
            long inventoryConfigurationId,
            long solutionCenterId,
            long sectionId)
        {
            var assignment =
                await _context.InventoryConfigurationAssignments
                    .FirstOrDefaultAsync(item =>
                        item.inventory_configuration_id ==
                            inventoryConfigurationId
                        &&
                        item.solution_center_id ==
                            solutionCenterId
                        &&
                        item.section_id ==
                            sectionId);

            if (assignment is null)
                return false;

            _context.InventoryConfigurationAssignments
                .Remove(assignment);

            await _context.SaveChangesAsync();

            return true;
        }
        /// <summary>
        /// Elimina todas las asociaciones de una sección
        /// dentro de una configuración de inventario.
        /// </summary>
        public async Task<int> DeleteAssignmentsBySection(
            long inventoryConfigurationId,
            long sectionId)
        {
            var assignments =
                await _context.InventoryConfigurationAssignments
                    .Where(item =>
                        item.inventory_configuration_id ==
                            inventoryConfigurationId
                        &&
                        item.section_id ==
                            sectionId)
                    .ToListAsync();

            if (assignments.Count == 0)
                return 0;

            _context.InventoryConfigurationAssignments
                .RemoveRange(assignments);

            await _context.SaveChangesAsync();

            return assignments.Count;
        }
    }
}