using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class InventoryConfigurationApplication
        : IInventoryConfigurationApplication
    {
        private readonly IInventoryConfigurationRepository
            _inventoryConfigurationRepository;

        public InventoryConfigurationApplication(
            IInventoryConfigurationRepository
                inventoryConfigurationRepository)
        {
            _inventoryConfigurationRepository =
                inventoryConfigurationRepository;
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

                var normalizedRole =
                    role.Trim().ToUpperInvariant();

                var validRoles =
                    new[]
                    {
                "ALMACEN",
                "COSTOS",
                "CONTROL INTERNO",
                "ADMINISTRADOR"
                    };

                if (!validRoles.Contains(normalizedRole))
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

                // ALMACEN solamente puede consultar bodegas.
                if (normalizedRole == "ALMACEN" &&
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

                var normalizedRole =
                    role.Trim().ToUpperInvariant();

                var validRoles =
                    new[]
                    {
                "ALMACEN",
                "COSTOS",
                "CONTROL INTERNO",
                "ADMINISTRADOR"
                    };

                if (!validRoles.Contains(normalizedRole))
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

                // ALMACEN solamente puede visualizar Bodegas.
                if (normalizedRole == "ALMACEN")
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