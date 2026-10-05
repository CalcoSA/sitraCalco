using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Application.Constants;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Domain.Options;
using Microsoft.Extensions.Options;

namespace Inventory.Application.Services
{
    public class InventoryConfigurationApplication
        : IInventoryConfigurationApplication
    {
        private readonly IInventoryConfigurationRepository
            _inventoryConfigurationRepository;
        private readonly IPermissionApplication _permissionApplication;
        private readonly IStorageService? _storageService;
        private readonly GoogleCloudStorageOptions? _storageOptions;

        public InventoryConfigurationApplication(
            IInventoryConfigurationRepository
                inventoryConfigurationRepository,
            IPermissionApplication permissionApplication,
            IStorageService? storageService = null,
            IOptions<GoogleCloudStorageOptions>? storageOptions = null)
        {
            _inventoryConfigurationRepository =
                inventoryConfigurationRepository;
            _permissionApplication = permissionApplication;
            _storageService = storageService;
            _storageOptions = storageOptions?.Value;
        }

        /// <summary>
        /// Crea una nueva configuración de inventario.
        /// </summary>
        public async Task<long> Create(
            CreateInventoryConfigurationDto request)
        {
            try
            {
                if (request is null)
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.InventoryConfigurationName))
                    return 0;

                if (request.StartDate.HasValue &&
                    request.EndDate.HasValue &&
                    request.StartDate.Value.Date >
                    request.EndDate.Value.Date)
                {
                    return 0;
                }

                var configurationName =
                    request.InventoryConfigurationName.Trim();

                var nameExists =
                    await _inventoryConfigurationRepository
                        .ExistsByName(
                            configurationName);

                if (nameExists)
                    return 0;

                var inventoryConfiguration =
                    new InventoryConfiguration
                    {
                        inventory_configuration_name =
                            configurationName,

                        start_date =
                            request.StartDate?.Date,

                        end_date =
                            request.EndDate?.Date
                    };

                return await
                    _inventoryConfigurationRepository
                        .CreateInventoryConfiguration(
                            inventoryConfiguration);
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> CreateAssignments(
    long inventoryConfigurationId,
    CreateInventoryConfigurationAssignmentsDto request)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                    return 0;

                if (request is null)
                    return 0;

                if (request.Assignments is null ||
                    request.Assignments.Count == 0)
                    return 0;

                // Validar que la configuración exista.
                var configurationExists =
                    await _inventoryConfigurationRepository
                        .ConfigurationExists(
                            inventoryConfigurationId);

                if (!configurationExists)
                    return 0;

                // Protección adicional por si Application
                // fuera invocada sin pasar por FluentValidation.
                var duplicatedAssignments =
                    request.Assignments
                        .GroupBy(assignment => new
                        {
                            assignment.SolutionCenterId,
                            assignment.SectionId
                        })
                        .Any(group =>
                            group.Count() > 1);

                if (duplicatedAssignments)
                    return 0;

                var assignmentsToCreate =
                    new List<InventoryConfigurationAssignment>();

                // IMPORTANTE:
                // Primero validamos TODAS las asociaciones.
                foreach (var assignment in request.Assignments)
                {
                    if (assignment.SolutionCenterId <= 0 ||
                        assignment.SectionId <= 0)
                    {
                        return 0;
                    }

                    // Validar que exista la bodega
                    // o punto de venta.
                    var solutionCenterExists =
                        await _inventoryConfigurationRepository
                            .SolutionCenterExists(
                                assignment.SolutionCenterId);

                    if (!solutionCenterExists)
                        return 0;

                    // Validar que exista la sección.
                    var sectionExists =
                        await _inventoryConfigurationRepository
                            .SectionExists(
                                assignment.SectionId);

                    if (!sectionExists)
                        return 0;

                    // Validar que la sección realmente
                    // pertenezca a ese Solution Center.
                    var sectionBelongs =
                        await _inventoryConfigurationRepository
                            .SectionBelongsToSolutionCenter(
                                assignment.SolutionCenterId,
                                assignment.SectionId);

                    if (!sectionBelongs)
                        return 0;

                    // Validar que esta combinación
                    // aún no exista en la configuración.
                    var assignmentExists =
                        await _inventoryConfigurationRepository
                            .AssignmentExists(
                                inventoryConfigurationId,
                                assignment.SolutionCenterId,
                                assignment.SectionId);

                    if (assignmentExists)
                        return 0;

                    assignmentsToCreate.Add(
                        new InventoryConfigurationAssignment
                        {
                            inventory_configuration_id =
                                inventoryConfigurationId,

                            solution_center_id =
                                assignment.SolutionCenterId,

                            section_id =
                                assignment.SectionId,

                            // Toda asociación nueva se crea activa.
                            is_active = true
                        });
                }

                // Solo llegamos aquí cuando TODAS
                // las asociaciones pasaron las validaciones.
                return await _inventoryConfigurationRepository
                    .CreateAssignments(
                        assignmentsToCreate);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Obtiene las opciones disponibles para la
        /// configuración de inventarios.
        /// 1: Bodegas.
        /// 2: Puntos de Venta.
        /// 3: Secciones exclusivas de Puntos de Venta.
        /// </summary>
        public async Task<object?> GetOptions(
            int type)
        {
            try
            {
                switch (type)
                {
                    case 1:
                        return await _inventoryConfigurationRepository
                            .GetSolutionCentersByType(1);

                    case 2:
                        return await _inventoryConfigurationRepository
                            .GetSolutionCentersByType(2);

                    case 3:
                        return await _inventoryConfigurationRepository
                            .GetPointOfSaleOnlySections();

                    default:
                        return null;
                }
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// Obtiene una bodega o punto de venta
        /// junto con todas sus secciones asociadas.
        /// </summary>
        public async Task<SolutionCenterSectionsDto?>
            GetSolutionCenterWithSections(
                long solutionCenterId)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return null;

                return await _inventoryConfigurationRepository
                    .GetSolutionCenterWithSections(
                        solutionCenterId);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// Obtiene una sección junto con todos los
        /// Puntos de Venta asociados.
        /// </summary>
        public async Task<SectionSolutionCentersDto?>
            GetSectionWithPointOfSales(
                long sectionId)
        {
            try
            {
                if (sectionId <= 0)
                    return null;

                return await _inventoryConfigurationRepository
                    .GetSectionWithPointOfSales(
                        sectionId);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// Obtiene las configuraciones disponibles hoy para realizar inventario en el centro.
        /// </summary>
        public async Task<AvailableInventoryConfigurationsResultDto> GetAvailableInventoryConfigurations(
            long solutionCenterId,
            string role)
        {
            var result = new AvailableInventoryConfigurationsResultDto();

            if (solutionCenterId <= 0 || string.IsNullOrWhiteSpace(role))
                return result;

            var canViewAll = await _permissionApplication.HasPermission(
                PermissionKeys.ViewAllSolutionCenters, role);
            var canViewWarehouses = !canViewAll && await _permissionApplication.HasPermission(
                PermissionKeys.ViewWarehousesOnly, role);

            if (!canViewAll && !canViewWarehouses)
                return result;

            result.IsValidRole = true;

            var solutionCenter = await _inventoryConfigurationRepository.GetSolutionCenterById(solutionCenterId);
            if (solutionCenter is null)
            {
                result.IsAllowed = true;
                return result;
            }

            const long warehouseTypeId = 1;
            if (!canViewAll && solutionCenter.solution_center_type_id != warehouseTypeId)
                return result;

            result.IsAllowed = true;
            result.Data = new List<AvailableInventoryConfigurationDto>();

            if (!solutionCenter.is_active)
                return result;

            // Mantener la convención de hora local del servidor utilizada por Inventory.
            // Una sola lectura evita inconsistencias entre fecha y día al cruzar medianoche.
            var today = DateTime.Today;
            var currentDay = today.DayOfWeek switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miercoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sabado",
                DayOfWeek.Sunday => "Domingo",
                _ => throw new InvalidOperationException("El día actual no es válido.")
            };

            var configurations = await _inventoryConfigurationRepository.GetAvailabilityCandidates(solutionCenterId);

            result.Data = configurations
                .Where(configuration =>
                    (!configuration.StartDate.HasValue || today >= configuration.StartDate.Value.Date) &&
                    (!configuration.EndDate.HasValue || today <= configuration.EndDate.Value.Date) &&
                    (configuration.Days.Count == 0 || configuration.Days.Any(day =>
                        string.Equals(NormalizeDay(day), currentDay, StringComparison.OrdinalIgnoreCase))))
                .OrderBy(configuration => configuration.InventoryConfigurationName)
                .Select(configuration => new AvailableInventoryConfigurationDto
                {
                    InventoryConfigurationId = configuration.InventoryConfigurationId,
                    InventoryConfigurationName = configuration.InventoryConfigurationName
                })
                .ToList();

            return result;
        }

        /// <summary>
        /// Obtiene las secciones activas de una configuración disponible hoy para el centro.
        /// </summary>
        public async Task<AvailableInventorySectionsResultDto> GetAvailableInventorySections(
            long solutionCenterId,
            long inventoryConfigurationId,
            string role)
        {
            var result = new AvailableInventorySectionsResultDto();

            if (solutionCenterId <= 0 || inventoryConfigurationId <= 0 || string.IsNullOrWhiteSpace(role))
                return result;

            // Revalidar las mismas reglas de /available, sin depender de una consulta previa del frontend.
            var availability = await GetAvailableInventoryConfigurations(solutionCenterId, role);
            result.IsValidRole = availability.IsValidRole;
            result.IsAllowed = availability.IsAllowed;

            if (!result.IsValidRole || !result.IsAllowed || availability.Data is null)
                return result;

            result.SolutionCenterExists = true;
            result.ConfigurationExists = await _inventoryConfigurationRepository.ConfigurationExists(inventoryConfigurationId);

            if (!result.ConfigurationExists)
                return result;

            result.IsAvailable = availability.Data.Any(configuration =>
                configuration.InventoryConfigurationId == inventoryConfigurationId);

            if (!result.IsAvailable)
                return result;

            result.Data = (await _inventoryConfigurationRepository
                .GetAvailableInventorySections(solutionCenterId, inventoryConfigurationId)).ToList();

            return result;
        }

        /// <summary>
        /// Obtiene una página de productos de una sección disponible para la configuración y el centro.
        /// </summary>
        public async Task<AvailableInventoryProductsResultDto> GetAvailableInventoryProducts(
            long solutionCenterId,
            long inventoryConfigurationId,
            long sectionId,
            string role,
            int page,
            int take)
        {
            var result = new AvailableInventoryProductsResultDto();

            if (solutionCenterId <= 0 || inventoryConfigurationId <= 0 || sectionId <= 0 ||
                page <= 0 || take <= 0 || string.IsNullOrWhiteSpace(role))
                return result;

            // Reutilizar toda la cadena de permisos, disponibilidad y asignaciones del punto de entrada de secciones.
            var availability = await GetAvailableInventorySections(solutionCenterId, inventoryConfigurationId, role);
            result.IsValidRole = availability.IsValidRole;
            result.IsAllowed = availability.IsAllowed;
            result.SolutionCenterExists = availability.SolutionCenterExists;
            result.ConfigurationExists = availability.ConfigurationExists;
            result.IsAvailable = availability.IsAvailable;

            if (!result.IsValidRole || !result.IsAllowed)
                return result;

            var canViewFullDetail = await _permissionApplication.HasPermission(
                PermissionKeys.ViewFullInventoryDetail, role);
            var canViewLimitedDetail = !canViewFullDetail && await _permissionApplication.HasPermission(
                PermissionKeys.ViewLimitedInventoryDetail, role);

            // Ambos permisos habilitan los datos base; aún no hay conteos ni datos ERP integrados que diferenciar.
            result.HasDetailPermission = canViewFullDetail || canViewLimitedDetail;
            if (!result.HasDetailPermission || !result.SolutionCenterExists ||
                !result.ConfigurationExists || !result.IsAvailable)
                return result;

            result.IsSectionAvailable = availability.Data.Any(section => section.SectionId == sectionId);
            if (!result.IsSectionAvailable)
                return result;

            result.Data = await _inventoryConfigurationRepository
                .GetAvailableInventoryProducts(solutionCenterId, sectionId, page, take);

            var productsWithImages = result.Data.Items
                .Where(product => !string.IsNullOrWhiteSpace(product.ImagePath))
                .ToList();

            if (productsWithImages.Count > 0)
            {
                var storageService = _storageService ?? throw new InvalidOperationException(
                    "El servicio de almacenamiento es obligatorio para generar las URLs de las imágenes.");
                var storageOptions = _storageOptions ?? throw new InvalidOperationException(
                    "La configuración de almacenamiento es obligatoria para generar las URLs de las imágenes.");
                var expiresAt = DateTimeOffset.UtcNow.AddMinutes(storageOptions.SignedUrlExpirationMinutes);

                foreach (var product in productsWithImages)
                {
                    product.ImageUrl = await storageService.GenerateSignedUrlAsync(product.ImagePath!, expiresAt);
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene las configuraciones de inventario asociadas
        /// a una bodega o punto de venta aplicando permisos por rol.
        /// </summary>
        public async Task<SolutionCenterInventoryConfigurationsResultDto>
            GetInventoryConfigurationsBySolutionCenterId(
                long solutionCenterId,
                string role)
        {
            try
            {
                var result =
                    new SolutionCenterInventoryConfigurationsResultDto
                    {
                        IsValidRole = false,
                        IsAllowed = false,
                        Data = null
                    };

                if (solutionCenterId <= 0)
                    return result;

                if (string.IsNullOrWhiteSpace(role))
                    return result;

                var canViewAll = await _permissionApplication.HasPermission(
                    PermissionKeys.ViewAllSolutionCenters, role);
                var canViewWarehouses = !canViewAll && await _permissionApplication.HasPermission(
                    PermissionKeys.ViewWarehousesOnly, role);

                if (!canViewAll && !canViewWarehouses)
                    return result;

                result.IsValidRole = true;

                var data =
                    await _inventoryConfigurationRepository
                        .GetInventoryConfigurationsBySolutionCenterId(
                            solutionCenterId);

                // El Solution Center no existe.
                if (data is null)
                {
                    result.IsAllowed = true;
                    return result;
                }

                const long warehouseTypeId = 1;

                // La visibilidad de bodegas se obtiene de Permission, no del nombre del rol.
                if (!canViewAll &&
                    data.SolutionCenterTypeId != warehouseTypeId)
                {
                    result.IsAllowed = false;
                    return result;
                }

                result.IsAllowed = true;
                result.Data = data;

                return result;
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// Obtiene una configuración de inventario por su identificador
        /// aplicando visibilidad según el rol.
        /// </summary>
        public async Task<InventoryConfigurationByIdResultDto>
            GetInventoryConfigurationById(
                long inventoryConfigurationId,
                string role)
        {
            try
            {
                var result =
                    new InventoryConfigurationByIdResultDto
                    {
                        IsValidRole = false,
                        Data = null
                    };

                if (inventoryConfigurationId <= 0)
                    return result;

                if (string.IsNullOrWhiteSpace(role))
                    return result;

                var canViewAll = await _permissionApplication.HasPermission(
                    PermissionKeys.ViewAllSolutionCenters, role);
                var canViewWarehouses = !canViewAll && await _permissionApplication.HasPermission(
                    PermissionKeys.ViewWarehousesOnly, role);

                if (!canViewAll && !canViewWarehouses)
                    return result;

                result.IsValidRole = true;

                var data =
                    await _inventoryConfigurationRepository
                        .GetInventoryConfigurationById(
                            inventoryConfigurationId);

                // La configuración no existe.
                if (data is null)
                    return result;

                const long warehouseTypeId = 1;

                // Mantener el filtro actual según el permiso de visibilidad configurado.
                if (!canViewAll)
                {
                    data.SolutionCenters =
                        data.SolutionCenters
                            .Where(solutionCenter =>
                                solutionCenter.SolutionCenterTypeId ==
                                warehouseTypeId)
                            .ToList();
                }

                result.Data = data;

                return result;
            }
            catch
            {
                throw;
            }
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
            try
            {
                if (inventoryConfigurationId <= 0)
                    return false;

                if (solutionCenterId <= 0)
                    return false;

                if (sectionId <= 0)
                    return false;

                return await _inventoryConfigurationRepository
                    .UpdateAssignmentStatus(
                        inventoryConfigurationId,
                        solutionCenterId,
                        sectionId,
                        isActive);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// Actualiza el nombre y el rango de fechas
        /// de una configuración de inventario.
        /// </summary>
        public async Task<bool> UpdateInventoryConfiguration(
            long inventoryConfigurationId,
            UpdateInventoryConfigurationDto request)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                    return false;

                if (request is null)
                    return false;

                if (string.IsNullOrWhiteSpace(
                    request.InventoryConfigurationName))
                    return false;

                if (request.StartDate.HasValue &&
                    request.EndDate.HasValue &&
                    request.StartDate.Value.Date >
                    request.EndDate.Value.Date)
                {
                    return false;
                }

                // Validar que la configuración exista.
                var configurationExists =
                    await _inventoryConfigurationRepository
                        .ConfigurationExists(
                            inventoryConfigurationId);

                if (!configurationExists)
                    return false;

                var configurationName =
                    request.InventoryConfigurationName
                        .Trim();

                // Validar que no exista OTRA configuración
                // con el mismo nombre.
                var nameExists =
                    await _inventoryConfigurationRepository
                        .ExistsByName(
                            configurationName,
                            inventoryConfigurationId);

                if (nameExists)
                    return false;

                return await _inventoryConfigurationRepository
                    .UpdateInventoryConfiguration(
                        inventoryConfigurationId,
                        configurationName,
                        request.StartDate,
                        request.EndDate);
            }
            catch
            {
                throw;
            }
        }
        private static string? NormalizeDay(
    string day)
        {
            return day
                .Trim()
                .ToUpperInvariant() switch
            {
                "LUNES" =>
                    "Lunes",

                "MARTES" =>
                    "Martes",

                "MIERCOLES" =>
                    "Miercoles",

                "JUEVES" =>
                    "Jueves",

                "VIERNES" =>
                    "Viernes",

                "SABADO" =>
                    "Sabado",

                "DOMINGO" =>
                    "Domingo",

                _ => null
            };
        }
        /// <summary>
        /// Agrega uno o varios días a una configuración
        /// de inventario.
        /// </summary>
        public async Task<int> AddDays(
            long inventoryConfigurationId,
            AddInventoryConfigurationDaysDto request)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                    return 0;

                if (request is null)
                    return 0;

                if (request.Days is null ||
                    request.Days.Count == 0)
                    return 0;

                // Validar que la configuración exista.
                var configurationExists =
                    await _inventoryConfigurationRepository
                        .ConfigurationExists(
                            inventoryConfigurationId);

                if (!configurationExists)
                    return 0;

                // Protección adicional contra días repetidos
                // dentro del mismo request.
                var duplicatedDays =
                    request.Days
                        .Select(day =>
                            day.Trim().ToUpperInvariant())
                        .GroupBy(day => day)
                        .Any(group =>
                            group.Count() > 1);

                if (duplicatedDays)
                    return 0;

                var daysToCreate =
                    new List<InventoryConfigurationDay>();

                // Primero validamos TODOS los días.
                foreach (var day in request.Days)
                {
                    if (string.IsNullOrWhiteSpace(day))
                        return 0;

                    var normalizedDay =
                        NormalizeDay(day);

                    if (normalizedDay is null)
                        return 0;

                    var dayExists =
                        await _inventoryConfigurationRepository
                            .DayExists(
                                inventoryConfigurationId,
                                normalizedDay);

                    if (dayExists)
                        return 0;

                    daysToCreate.Add(
                        new InventoryConfigurationDay
                        {
                            inventory_configuration_id =
                                inventoryConfigurationId,

                            day_of_week =
                                normalizedDay
                        });
                }

                // Solo guardamos cuando TODOS pasaron
                // correctamente las validaciones.
                return await _inventoryConfigurationRepository
                    .AddDays(daysToCreate);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// Elimina un día específico de una
        /// configuración de inventario.
        /// </summary>
        public async Task<bool> DeleteDay(
            long inventoryConfigurationId,
            string dayOfWeek)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                    return false;

                if (string.IsNullOrWhiteSpace(dayOfWeek))
                    return false;

                var configurationExists =
                    await _inventoryConfigurationRepository
                        .ConfigurationExists(
                            inventoryConfigurationId);

                if (!configurationExists)
                    return false;

                var normalizedDay =
                    NormalizeDay(dayOfWeek);

                if (normalizedDay is null)
                    return false;

                return await _inventoryConfigurationRepository
                    .DeleteDay(
                        inventoryConfigurationId,
                        normalizedDay);
            }
            catch
            {
                throw;
            }
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
            try
            {
                if (inventoryConfigurationId <= 0)
                    return false;

                if (solutionCenterId <= 0)
                    return false;

                if (sectionId <= 0)
                    return false;

                return await _inventoryConfigurationRepository
                    .DeleteAssignment(
                        inventoryConfigurationId,
                        solutionCenterId,
                        sectionId);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// Elimina una sección de todos los Solution Centers
        /// asociados dentro de una configuración de inventario.
        /// </summary>
        public async Task<int> DeleteAssignmentsBySection(
            long inventoryConfigurationId,
            long sectionId)
        {
            try
            {
                if (inventoryConfigurationId <= 0)
                    return 0;

                if (sectionId <= 0)
                    return 0;

                var configurationExists =
                    await _inventoryConfigurationRepository
                        .ConfigurationExists(
                            inventoryConfigurationId);

                if (!configurationExists)
                    return 0;

                var sectionExists =
                    await _inventoryConfigurationRepository
                        .SectionExists(
                            sectionId);

                if (!sectionExists)
                    return 0;

                return await _inventoryConfigurationRepository
                    .DeleteAssignmentsBySection(
                        inventoryConfigurationId,
                        sectionId);
            }
            catch
            {
                throw;
            }
        }
    }
}
