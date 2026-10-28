using Inventory.Domain.Helpers;
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
            return await (
                from assignment in _context.SolutionCenterSections.AsNoTracking()
                join center in _context.SolutionCenters.AsNoTracking()
                    on assignment.solution_center_id equals center.solution_center_id
                join section in _context.Sections.AsNoTracking()
                    on assignment.section_id equals section.section_id
                where assignment.solution_center_id == solutionCenterId &&
                    assignment.section_id == sectionId && assignment.is_active &&
                    section.solution_center_type_id == center.solution_center_type_id
                select assignment.solution_center_section_id
            ).AnyAsync();
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
        public async Task<AuditResult<int>> CreateAssignments(
            IEnumerable<InventoryConfigurationAssignment> assignments)
        {
            var assignmentsToCreate =
                assignments.ToList();

            if (assignmentsToCreate.Count == 0)
                return new(0, "Debe enviar al menos una asignación.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            // Orden común para lotes que abarcan varias parejas. Tomar todos los
            // bloqueos antes de las lecturas evita snapshots anteriores a una espera.
            var pairs = assignmentsToCreate
                .Select(item => (item.solution_center_id, item.section_id)).Distinct()
                .OrderBy(pair => pair.solution_center_id).ThenBy(pair => pair.section_id).ToList();
            var centerIds = pairs.Select(pair => pair.solution_center_id).Distinct().ToList();
            var sectionIds = pairs.Select(pair => pair.section_id).Distinct().ToList();
            List<SolutionCenterSection> centerSections;
            if (_context.Database.IsMySql())
            {
                // Equivalente en lote a LockSectionAssignment: mismas parejas,
                // FOR UPDATE y parámetros, sin una consulta por centro/sección.
                var predicates = pairs.Select((pair, index) =>
                    $"(solution_center_id = {{{index * 2}}} AND section_id = {{{index * 2 + 1}}})");
                var parameters = pairs.SelectMany(pair => new object[] { pair.solution_center_id, pair.section_id }).ToArray();
                var sql = "SELECT solution_center_section_id, solution_center_id, section_id, is_active " +
                    "FROM sitracalco_inventory_solution_center_section WHERE " + string.Join(" OR ", predicates) +
                    " ORDER BY solution_center_id, section_id FOR UPDATE";
                centerSections = await _context.SolutionCenterSections.FromSqlRaw(sql, parameters)
                    .AsNoTracking().ToListAsync();
            }
            else if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                centerSections = await _context.SolutionCenterSections.AsNoTracking()
                    .Where(item => centerIds.Contains(item.solution_center_id) && sectionIds.Contains(item.section_id))
                    .OrderBy(item => item.solution_center_id).ThenBy(item => item.section_id).ToListAsync();
            }
            else
            {
                throw new NotSupportedException("El proveedor no admite el bloqueo de secciones configurado.");
            }

            var configurationId = assignmentsToCreate[0].inventory_configuration_id;
            var configurationName = await _context.InventoryConfigurations
                .Where(configuration => configuration.inventory_configuration_id == configurationId)
                .Select(configuration => configuration.inventory_configuration_name)
                .SingleOrDefaultAsync();
            if (configurationName is null)
                return new(0, $"No se pudieron guardar las asignaciones. La configuración de inventario con ID {configurationId} no existe.");
            var failurePrefix = $"No se pudieron guardar las asignaciones de la configuración {configurationName}.";

            var centers = await _context.SolutionCenters.AsNoTracking()
                .Where(center => centerIds.Contains(center.solution_center_id))
                .Select(center => new { center.solution_center_id, center.solution_center_code, center.solution_center_name, center.solution_center_type_id })
                .ToDictionaryAsync(center => center.solution_center_id);
            var missingCenters = centerIds.Where(id => !centers.ContainsKey(id)).ToList();
            if (missingCenters.Count > 0)
                return new(0, $"{failurePrefix} No existen los centros de solución con ID: {string.Join(", ", missingCenters)}.");

            var sections = await _context.Sections.AsNoTracking()
                .Where(section => sectionIds.Contains(section.section_id))
                .Select(section => new { section.section_id, section.section_name, section.solution_center_type_id })
                .ToDictionaryAsync(section => section.section_id);
            var missingSections = sectionIds.Where(id => !sections.ContainsKey(id)).ToList();
            if (missingSections.Count > 0)
                return new(0, $"{failurePrefix} No existen las secciones con ID: {string.Join(", ", missingSections)}.");

            string CenterLabel(long id) => $"{centers[id].solution_center_code} - {centers[id].solution_center_name}";
            string SectionLabel(long id) => $"{sections[id].section_name} (ID {id})";
            string PairLabel((long solution_center_id, long section_id) pair) =>
                $"{CenterLabel(pair.solution_center_id)} / sección {SectionLabel(pair.section_id)}";

            var relations = centerSections.ToDictionary(item => (item.solution_center_id, item.section_id));
            var unassigned = pairs.Where(pair => !relations.ContainsKey(pair)).ToList();
            if (unassigned.Count > 0)
                return new(0, $"{failurePrefix} Las siguientes secciones no están asociadas al centro indicado: {string.Join("; ", unassigned.Select(PairLabel))}.");
            var inactive = pairs.Where(pair => !relations[pair].is_active).ToList();
            if (inactive.Count > 0)
                return new(0, $"{failurePrefix} Las siguientes relaciones centro–sección están inactivas: {string.Join("; ", inactive.Select(PairLabel))}.");
            var incompatible = pairs.Where(pair => sections[pair.section_id].solution_center_type_id != centers[pair.solution_center_id].solution_center_type_id).ToList();
            if (incompatible.Count > 0)
                return new(0, $"{failurePrefix} Las siguientes secciones no corresponden al tipo del centro indicado: {string.Join("; ", incompatible.Select(PairLabel))}.");

            // Consultar candidatas en lote y conservar únicamente las parejas exactas
            // solicitadas. Una sección diferente no constituye un duplicado.
            var existing = await _context.InventoryConfigurationAssignments.AsNoTracking()
                .Where(item => item.inventory_configuration_id == configurationId &&
                    centerIds.Contains(item.solution_center_id) && sectionIds.Contains(item.section_id))
                .Select(item => new { item.solution_center_id, item.section_id, item.is_active }).ToListAsync();
            var requestedPairs = pairs.ToHashSet();
            var conflicts = existing.Where(item => requestedPairs.Contains((item.solution_center_id, item.section_id))).ToList();
            if (conflicts.Count > 0)
            {
                var details = conflicts.GroupBy(item => item.section_id).OrderBy(group => group.Key)
                    .Select(group => $"sección {SectionLabel(group.Key)}: {string.Join(", ", group.Select(item => item.solution_center_id).Distinct().OrderBy(id => centers[id].solution_center_code, StringComparer.Ordinal).Select(CenterLabel))}");
                var message = $"No se pudieron guardar las asignaciones. Los siguientes puntos de venta o bodegas ya tienen asignada la configuración {configurationName}: {string.Join("; ", details)}. Desmarca estos centros y vuelve a intentarlo.";
                if (conflicts.Any(item => !item.is_active))
                    message += " Las asignaciones inactivas también están registradas; utiliza la opción de reactivación si corresponde.";
                // Este diagnóstico no es un log de auditoría: no truncar la lista de conflictos.
                return new(0, message);
            }

            await _context
                .InventoryConfigurationAssignments
                .AddRangeAsync(assignmentsToCreate);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new(assignmentsToCreate.Count, AuditDescription.Format(
                $"Se agregaron {assignmentsToCreate.Count} asignaciones a la configuración de inventario {AuditDescription.Name(configurationName, assignmentsToCreate[0].inventory_configuration_id)}."));
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
                        SolutionCenterTypeId = section.solution_center_type_id,

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
                            SolutionCenterTypeId = section.solution_center_type_id,

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
        /// Obtiene el centro y su estado sin cargar secciones ni productos.
        /// </summary>
        public async Task<SolutionCenter?> GetSolutionCenterById(long solutionCenterId)
        {
            return await _context.SolutionCenters.AsNoTracking()
                .FirstOrDefaultAsync(center => center.solution_center_id == solutionCenterId);
        }

        /// <summary>
        /// Obtiene una sola fila por configuración con alguna asignación activa en el centro.
        /// Las fechas y los días se evalúan posteriormente en Application.
        /// </summary>
        public async Task<IEnumerable<InventoryConfigurationAvailabilityDto>> GetAvailabilityCandidates(
            long solutionCenterId)
        {
            // Any se traduce a EXISTS: varias secciones activas no multiplican las configuraciones.
            var configurations = await _context.InventoryConfigurations.AsNoTracking()
            .Where(configuration => _context.InventoryConfigurationAssignments.Any(assignment =>
                assignment.inventory_configuration_id == configuration.inventory_configuration_id &&
                assignment.solution_center_id == solutionCenterId &&
                assignment.is_active &&
                _context.SolutionCenterSections.Any(solutionCenterSection =>
                    solutionCenterSection.solution_center_id == assignment.solution_center_id &&
                    solutionCenterSection.section_id == assignment.section_id &&
                    solutionCenterSection.is_active)))
            .OrderBy(configuration => configuration.inventory_configuration_name)
            .Select(configuration => new InventoryConfigurationAvailabilityDto
            {
                InventoryConfigurationId = configuration.inventory_configuration_id,
                InventoryConfigurationName = configuration.inventory_configuration_name,
                StartDay = configuration.start_day,
                EndDay = configuration.end_day
            })
            .ToListAsync();

            if (configurations.Count == 0)
                return configurations;

            var configurationIds = configurations.Select(configuration => configuration.InventoryConfigurationId).ToList();
            // Una consulta para todos los días, siguiendo el patrón del repositorio y sin N+1.
            var days = await _context.InventoryConfigurationDays.AsNoTracking()
                .Where(day => configurationIds.Contains(day.inventory_configuration_id))
                .Select(day => new
                {
                    day.inventory_configuration_id,
                    day.day_of_week
                })
                .ToListAsync();

            var daysByConfiguration = days.ToLookup(day => day.inventory_configuration_id, day => day.day_of_week);
            foreach (var configuration in configurations)
            {
                configuration.Days = daysByConfiguration[configuration.InventoryConfigurationId].ToList();
            }

            return configurations;
        }

        /// <summary>
        /// Obtiene secciones activas con una asignación activa para ambos IDs seleccionados.
        /// </summary>
        public async Task<IEnumerable<AvailableInventorySectionDto>> GetAvailableInventorySections(
            long solutionCenterId,
            long inventoryConfigurationId)
        {
            // Partir de Section y usar EXISTS evita duplicados por section_id y consultas N+1.
            return await _context.Sections.AsNoTracking()
            .Where(section => section.is_active &&
                _context.InventoryConfigurationAssignments.Any(assignment =>
                    assignment.solution_center_id == solutionCenterId &&
                    assignment.inventory_configuration_id == inventoryConfigurationId &&
                    assignment.section_id == section.section_id &&
                    assignment.is_active &&
                    _context.SolutionCenterSections.Any(solutionCenterSection =>
                        solutionCenterSection.solution_center_id == assignment.solution_center_id &&
                        solutionCenterSection.section_id == assignment.section_id &&
                        solutionCenterSection.is_active)))
            .OrderBy(section => section.section_name)
            .Select(section => new AvailableInventorySectionDto
            {
                SectionId = section.section_id,
                SectionName = section.section_name
            })
            .ToListAsync();
        }

        /// <summary>
        /// Obtiene los productos del centro y sección con paginación y sin cargar datos administrativos.
        /// </summary>
        public async Task<PagedDto<AvailableInventoryProductDto>> GetAvailableInventoryProducts(
            long solutionCenterId,
            long sectionId,
            int page,
            int take)
        {
            var query =
                from association in _context.SolutionCenterProducts.AsNoTracking()
                join product in _context.Products.AsNoTracking()
                    on association.product_id equals product.product_id
                where association.solution_center_id == solutionCenterId &&
                    association.section_id == sectionId
                select new AvailableInventoryProductDto
                {
                    SolutionCenterProductId = association.solution_center_product_id,
                    ProductId = product.product_id,
                    ImagePath = product.image_path,
                    Reference = product.reference,
                    ProductName = product.product_name,
                    UnitOfMeasure = product.unit_of_measure,
                    PlanId = product.plan_id,
                    SortOrder = association.sort_order
                };

            var total = await query.CountAsync();
            var result = new PagedDto<AvailableInventoryProductDto>
            {
                Total = total,
                Page = page,
                Take = take,
                Pages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)take)
            };

            // Evitar desbordamiento del offset y conservar los totales si se pide una página fuera de rango.
            var offset = ((long)page - 1) * take;
            if (offset >= total)
                return result;

            result.Items = await query
                .OrderBy(product => product.SortOrder)
                .ThenBy(product => product.SolutionCenterProductId)
                .Skip((int)offset)
                .Take(take)
                .ToListAsync();

            return result;
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

                    join centerSection in
                        _context.SolutionCenterSections.AsNoTracking()

                    on new { assignment.solution_center_id, assignment.section_id }
                        equals new { centerSection.solution_center_id, centerSection.section_id }
                        into centerSections

                    from centerSection in centerSections.DefaultIfEmpty()

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

                        StartDay =
                            configuration.start_day,

                        EndDay =
                            configuration.end_day,

                        SectionId =
                            section.section_id,

                        SectionName =
                            section.section_name,

                        SectionIsActive =
                            (bool?)centerSection.is_active,

                        AssignmentIsActive =
                            assignment.is_active
                    }
                ).ToListAsync();

            // No ocultar asociaciones inconsistentes ni inventar su estado.
            if (assignmentRows.Any(item => !item.SectionIsActive.HasValue))
                throw new InvalidOperationException(
                    $"Existe una configuración con una sección sin relación con el centro {solutionCenterId}.");

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

                        InventoryConfigurationDayId = day.inventory_configuration_day_id,

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
                        item.StartDay,
                        item.EndDay
                    })
                    .Select(group =>
                        new InventoryConfigurationDetailDto
                        {
                            InventoryConfigurationId =
                                group.Key.InventoryConfigurationId,

                            InventoryConfigurationName =
                                group.Key.InventoryConfigurationName,

                            StartDay =
                                group.Key.StartDay,

                            EndDay =
                                group.Key.EndDay,

                            DaysOfWeek = dayRows
                                .Where(day => day.InventoryConfigurationId == group.Key.InventoryConfigurationId)
                                .Select(day => new InventoryConfigurationDayDto
                                {
                                    InventoryConfigurationDayId = day.InventoryConfigurationDayId,
                                    DayOfWeek = day.DayOfWeek
                                }).ToList(),

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
                                                item.SectionIsActive!.Value,

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

                            StartDay =
                                item.start_day,

                            EndDay =
                                item.end_day
                        })
                    .FirstOrDefaultAsync();

            if (configuration is null)
                return null;

            // 2. Obtener los días asociados.
            configuration.DaysOfWeek =
                await _context.InventoryConfigurationDays
                    .AsNoTracking()
                    .Where(day =>
                        day.inventory_configuration_id ==
                        inventoryConfigurationId)
                    .OrderBy(day =>
                        day.inventory_configuration_day_id)
                    .Select(day => new InventoryConfigurationDayDto
                    {
                        InventoryConfigurationDayId = day.inventory_configuration_day_id,
                        DayOfWeek = day.day_of_week
                    })
                    .ToListAsync();

            configuration.Days = configuration.DaysOfWeek.Select(day => day.DayOfWeek).ToList();

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
        public async Task<AuditResult<bool>> UpdateAssignmentStatus(
            long inventoryConfigurationId,
            long solutionCenterId,
            long sectionId,
            bool isActive)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var sectionAssignment = await _context.LockSectionAssignment(solutionCenterId, sectionId);
            if (isActive && sectionAssignment is null)
                return new(false);

            var auditContext = await GetAssignmentAuditContext(
                inventoryConfigurationId, solutionCenterId, sectionId);
            var assignment = auditContext?.Assignment;

            if (assignment is null)
                return new(false);

            if (isActive && (!auditContext!.SectionTypeId.HasValue ||
                auditContext.SectionTypeId != auditContext.SolutionCenterTypeId))
                return new(false);

            assignment.is_active = isActive;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new(true, AuditDescription.Format(
                $"Se {(assignment.is_active ? "activó" : "inactivó")} la sección {AuditDescription.Name(auditContext!.SectionName, sectionId)} para el centro de solución {AuditDescription.Name(auditContext.SolutionCenterName, solutionCenterId)} en la configuración de inventario {AuditDescription.Name(auditContext.ConfigurationName, inventoryConfigurationId)}."));
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
        /// Actualiza el nombre y el rango de días mensuales
        /// de una configuración de inventario.
        /// </summary>
        public async Task<InventoryConfigurationDto?> UpdateInventoryConfiguration(
            long inventoryConfigurationId, UpdateInventoryConfigurationDto request)
        {
            var configuration = await _context.InventoryConfigurations.AsNoTracking()
                .FirstOrDefaultAsync(item => item.inventory_configuration_id == inventoryConfigurationId);
            if (configuration is null)
                return null;

            var name = request.HasName ? request.InventoryConfigurationName?.Trim() : configuration.inventory_configuration_name;
            var startDay = request.HasStartDay ? request.StartDay : configuration.start_day;
            var endDay = request.HasEndDay ? request.EndDay : configuration.end_day;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100 ||
                !MonthlyInventoryRange.IsValid(startDay, endDay))
                return null;

            if (request.HasName && await ExistsByName(name, inventoryConfigurationId))
                return null;

            // Comparar los valores leídos evita perder cambios de otro PATCH entre lectura y escritura.
            // Una sola sentencia guarda el rango completo de forma atómica, incluso sin CHECK en MySQL antiguo.
            var updated = await _context.InventoryConfigurations
                .Where(item => item.inventory_configuration_id == inventoryConfigurationId &&
                    item.inventory_configuration_name == configuration.inventory_configuration_name &&
                    item.start_day == configuration.start_day && item.end_day == configuration.end_day)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.inventory_configuration_name, name)
                    .SetProperty(item => item.start_day, startDay)
                    .SetProperty(item => item.end_day, endDay));

            return updated == 1 ? new InventoryConfigurationDto
            {
                InventoryConfigurationId = inventoryConfigurationId,
                InventoryConfigurationName = name,
                StartDay = startDay,
                EndDay = endDay
            } : null;
        }

        public async Task<PagedDto<InventoryConfigurationListDto>> GetInventoryConfigurations(int page, int take)
        {
            var query = _context.InventoryConfigurations.AsNoTracking();
            var total = await query.CountAsync();
            var items = await query.OrderBy(item => item.inventory_configuration_name)
                .ThenBy(item => item.inventory_configuration_id)
                .Skip(checked((page - 1) * take)).Take(take)
                .Select(item => new InventoryConfigurationListDto
                {
                    InventoryConfigurationId = item.inventory_configuration_id,
                    InventoryConfigurationName = item.inventory_configuration_name,
                    StartDay = item.start_day,
                    EndDay = item.end_day
                }).ToListAsync();

            if (items.Count > 0)
            {
                var ids = items.Select(item => item.InventoryConfigurationId).ToList();
                var days = await _context.InventoryConfigurationDays.AsNoTracking()
                    .Where(day => ids.Contains(day.inventory_configuration_id))
                    .OrderBy(day => day.inventory_configuration_day_id)
                    .Select(day => new
                    {
                        day.inventory_configuration_id,
                        Day = new InventoryConfigurationDayDto
                        {
                            InventoryConfigurationDayId = day.inventory_configuration_day_id,
                            DayOfWeek = day.day_of_week
                        }
                    }).ToListAsync();
                var lookup = days.ToLookup(day => day.inventory_configuration_id, day => day.Day);
                foreach (var item in items)
                    item.DaysOfWeek = lookup[item.InventoryConfigurationId].ToList();
            }

            return new PagedDto<InventoryConfigurationListDto>
            {
                Items = items, Total = total, Page = page, Take = take,
                Pages = (int)Math.Ceiling(total / (double)take)
            };
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
        public async Task<AuditResult<int>> AddDays(
            IEnumerable<InventoryConfigurationDay> days)
        {
            var daysToCreate =
                days.ToList();

            if (daysToCreate.Count == 0)
                return new(0);

            var configurationName = await _context.InventoryConfigurations
                .Where(configuration => configuration.inventory_configuration_id == daysToCreate[0].inventory_configuration_id)
                .Select(configuration => configuration.inventory_configuration_name)
                .SingleOrDefaultAsync();

            await _context.InventoryConfigurationDays
                .AddRangeAsync(daysToCreate);

            await _context.SaveChangesAsync();

            return new(daysToCreate.Count, AuditDescription.Format(
                $"Se agregaron los días {string.Join(", ", daysToCreate.Select(day => day.day_of_week))} a la configuración de inventario {AuditDescription.Name(configurationName, daysToCreate[0].inventory_configuration_id)}."));
        }
        /// <summary>
        /// Elimina un día específico de una
        /// configuración de inventario.
        /// </summary>
        public async Task<AuditResult<bool>> DeleteDay(
            long inventoryConfigurationId,
            string dayOfWeek)
        {
            var normalizedDay =
                dayOfWeek.Trim().ToUpper();

            var auditContext = await (
                from item in _context.InventoryConfigurationDays
                join configuration in _context.InventoryConfigurations
                    on item.inventory_configuration_id equals configuration.inventory_configuration_id into configurations
                from configuration in configurations.DefaultIfEmpty()
                where item.inventory_configuration_id == inventoryConfigurationId
                    && item.day_of_week.ToUpper() == normalizedDay
                select new { Day = item, ConfigurationName = configuration != null ? configuration.inventory_configuration_name : null })
                .FirstOrDefaultAsync();
            var day = auditContext?.Day;

            if (day is null)
                return new(false);

            var description = AuditDescription.Format(
                $"Se eliminó el día {day.day_of_week} de la configuración de inventario {AuditDescription.Name(auditContext!.ConfigurationName, inventoryConfigurationId)}.");

            _context.InventoryConfigurationDays
                .Remove(day);

            await _context.SaveChangesAsync();

            return new(true, description);
        }
        /// <summary>
        /// Elimina una asociación específica entre
        /// configuración de inventario, Solution Center y sección.
        /// </summary>
        public async Task<AuditResult<bool>> DeleteAssignment(
            long inventoryConfigurationId,
            long solutionCenterId,
            long sectionId)
        {
            var auditContext = await GetAssignmentAuditContext(
                inventoryConfigurationId, solutionCenterId, sectionId);
            var assignment = auditContext?.Assignment;

            if (assignment is null)
                return new(false);

            var description = AuditDescription.Format(
                $"Se eliminó la asociación de la sección {AuditDescription.Name(auditContext!.SectionName, sectionId)} del centro de solución {AuditDescription.Name(auditContext.SolutionCenterName, solutionCenterId)} en la configuración de inventario {AuditDescription.Name(auditContext.ConfigurationName, inventoryConfigurationId)}.");

            _context.InventoryConfigurationAssignments
                .Remove(assignment);

            await _context.SaveChangesAsync();

            return new(true, description);
        }
        /// <summary>
        /// Elimina todas las asociaciones de una sección
        /// dentro de una configuración de inventario.
        /// </summary>
        public async Task<AuditResult<int>> DeleteAssignmentsBySection(
            long inventoryConfigurationId,
            long sectionId)
        {
            var auditContexts = await (
                from assignment in _context.InventoryConfigurationAssignments
                join configuration in _context.InventoryConfigurations
                    on assignment.inventory_configuration_id equals configuration.inventory_configuration_id into configurations
                from configuration in configurations.DefaultIfEmpty()
                join section in _context.Sections on assignment.section_id equals section.section_id into sections
                from section in sections.DefaultIfEmpty()
                where assignment.inventory_configuration_id == inventoryConfigurationId && assignment.section_id == sectionId
                select new { Assignment = assignment,
                    ConfigurationName = configuration != null ? configuration.inventory_configuration_name : null,
                    SectionName = section != null ? section.section_name : null })
                .ToListAsync();
            var assignments = auditContexts.Select(context => context.Assignment).ToList();

            if (assignments.Count == 0)
                return new(0);

            var description = AuditDescription.Format(
                $"Se eliminó la sección {AuditDescription.Name(auditContexts[0].SectionName, sectionId)} de {assignments.Count} asociación(es) de la configuración de inventario {AuditDescription.Name(auditContexts[0].ConfigurationName, inventoryConfigurationId)}.");

            _context.InventoryConfigurationAssignments
                .RemoveRange(assignments);

            await _context.SaveChangesAsync();

            return new(assignments.Count, description);
        }
        /// <summary>
        /// Retira todas las asignaciones de una configuración en un centro, sin tocar sus dependencias.
        /// </summary>
        public async Task<AuditResult<int>> DeleteAssignmentsBySolutionCenter(
            long inventoryConfigurationId, long solutionCenterId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                _context.Database.IsMySql()
                    ? System.Data.IsolationLevel.RepeatableRead
                    : System.Data.IsolationLevel.Serializable);

            if (_context.Database.IsMySql())
            {
                // Mismas filas y orden que LockSectionAssignment, antes de cualquier lectura.
                // Bloquear el rango completo incluye secciones sin asignaciones actuales y
                // coordina altas/reactivaciones; REPEATABLE READ protege también los huecos.
                await _context.SolutionCenterSections.FromSqlInterpolated($"""
                    SELECT solution_center_section_id, solution_center_id, section_id, is_active
                    FROM sitracalco_inventory_solution_center_section
                    WHERE solution_center_id = {solutionCenterId}
                    ORDER BY section_id
                    FOR UPDATE
                    """).AsNoTracking().ToListAsync();
            }
            else if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // La transacción de escritura inmediata serializa las pruebas aisladas.
                await _context.SolutionCenterSections.AsNoTracking()
                    .Where(item => item.solution_center_id == solutionCenterId)
                    .OrderBy(item => item.section_id).ToListAsync();
            }
            else
            {
                throw new NotSupportedException("El proveedor no admite el bloqueo de secciones configurado.");
            }

            var configurationName = await _context.InventoryConfigurations.AsNoTracking()
                .Where(item => item.inventory_configuration_id == inventoryConfigurationId)
                .Select(item => item.inventory_configuration_name).SingleOrDefaultAsync();
            var centerName = await _context.SolutionCenters.AsNoTracking()
                .Where(item => item.solution_center_id == solutionCenterId)
                .Select(item => item.solution_center_name).SingleOrDefaultAsync();

            var deletedAssignments = await _context.InventoryConfigurationAssignments
                .Where(item => item.inventory_configuration_id == inventoryConfigurationId &&
                    item.solution_center_id == solutionCenterId)
                .ExecuteDeleteAsync();

            var description = deletedAssignments == 0 ? "" : AuditDescription.Format(
                $"Se retiró la configuración de inventario {AuditDescription.Name(configurationName, inventoryConfigurationId)} del centro de solución {AuditDescription.Name(centerName, solutionCenterId)} y se eliminaron {deletedAssignments} asignaciones de secciones (activas o inactivas).");

            await transaction.CommitAsync();

            return new(deletedAssignments, description);
        }

        private sealed record AssignmentAuditContext(
            InventoryConfigurationAssignment Assignment,
            string? ConfigurationName,
            string? SolutionCenterName,
            string? SectionName,
            long? SolutionCenterTypeId,
            long? SectionTypeId);

        private Task<AssignmentAuditContext?> GetAssignmentAuditContext(
            long inventoryConfigurationId, long solutionCenterId, long sectionId)
        {
            return (
                from assignment in _context.InventoryConfigurationAssignments
                join configuration in _context.InventoryConfigurations
                    on assignment.inventory_configuration_id equals configuration.inventory_configuration_id into configurations
                from configuration in configurations.DefaultIfEmpty()
                join center in _context.SolutionCenters on assignment.solution_center_id equals center.solution_center_id into centers
                from center in centers.DefaultIfEmpty()
                join section in _context.Sections on assignment.section_id equals section.section_id into sections
                from section in sections.DefaultIfEmpty()
                where assignment.inventory_configuration_id == inventoryConfigurationId
                    && assignment.solution_center_id == solutionCenterId && assignment.section_id == sectionId
                select new AssignmentAuditContext(assignment,
                    configuration != null ? configuration.inventory_configuration_name : null,
                    center != null ? center.solution_center_name : null,
                    section != null ? section.section_name : null,
                    center != null ? (long?)center.solution_center_type_id : null,
                    section != null ? (long?)section.solution_center_type_id : null))
                .FirstOrDefaultAsync();
        }
    }
}
