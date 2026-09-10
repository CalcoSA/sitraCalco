using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;

namespace Inventory.Application.Services
{
    public class SolutionCenterApplication : ISolutionCenterApplication
    {
        private readonly ISolutionCenterRepository _solutionCenterRepository;
        private readonly IProductRepository    _productRepository;

        public SolutionCenterApplication(
    ISolutionCenterRepository solutionCenterRepository,
    IProductRepository productRepository)
        {
            _solutionCenterRepository =
                solutionCenterRepository;

            _productRepository =
                productRepository;
        }

        /// <summary>
        /// Obtiene los tipos de centros de soluciones registrados.
        /// </summary>
        /// <returns>
        /// Lista de tipos de centros de soluciones.
        /// </returns>
        public async Task<IEnumerable<SolutionCenterType>>
            GetSolutionCenterTypes()
        {
            try
            {
                return await _solutionCenterRepository
                    .GetSolutionCenterTypes();
            }
            catch
            {
                throw;
            }
        }

        public async Task<long> CreateSolutionCenter(
    CreateSolutionCenterDto request)
        {
            try
            {
                if (request is null)
                    return 0;

                if (request.SolutionCenterTypeId <= 0)
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.SolutionCenterCode))
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.SolutionCenterName))
                    return 0;

                // Validar que el tipo de centro exista.
                var typeExists =
                    await _solutionCenterRepository
                        .SolutionCenterTypeExists(
                            request.SolutionCenterTypeId);

                if (!typeExists)
                    return 0;

                // Normalizar código.
                var code = request.SolutionCenterCode
                    .Trim()
                    .ToUpperInvariant();

                // Validar que el código no exista.
                var codeExists =
                    await _solutionCenterRepository
                        .SolutionCenterCodeExists(code);

                if (codeExists)
                    return 0;

                // Normalizar nombre.
                var name = request.SolutionCenterName
                    .Trim();

                // Validar que el nombre no exista,
                // sin importar si es Bodega o Punto de Venta.
                var nameExists =
                    await _solutionCenterRepository
                        .SolutionCenterNameExists(name);

                if (nameExists)
                    return 0;

                // Construir la entidad que se guardará en MySQL.
                var solutionCenter = new SolutionCenter
                {
                    solution_center_type_id =
                        request.SolutionCenterTypeId,

                    solution_center_code = code,

                    solution_center_name = name,

                    // Todo centro de soluciones se crea activo.
                    is_active = true
                };

                // Guardar y retornar el ID autogenerado.
                return await _solutionCenterRepository
                    .CreateSolutionCenter(solutionCenter);
            }
            catch
            {
                throw;
            }
        }

        public async Task<long> CreateSectionConfiguration(
    long solutionCenterId,
    CreateSectionConfigurationDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return 0;

                if (request is null)
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.SectionName))
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.CreatedBy))
                    return 0;

                if (request.Products is null ||
                    request.Products.Count == 0)
                    return 0;

                // Validar existencia del centro.
                var solutionCenterExists =
                    await _solutionCenterRepository
                        .SolutionCenterExists(
                            solutionCenterId);

                if (!solutionCenterExists)
                    return 0;

                var sectionName =
                    request.SectionName.Trim();

                // No permitir dos secciones con el mismo
                // nombre dentro del mismo centro.
                var sectionNameExists =
                    await _solutionCenterRepository
                        .SectionNameExists(
                            solutionCenterId,
                            sectionName);

                if (sectionNameExists)
                    return 0;

                // ProductId y SortOrder deben ser válidos.
                var invalidProducts =
                    request.Products.Any(product =>
                        product.ProductId <= 0 ||
                        product.SortOrder <= 0);

                if (invalidProducts)
                    return 0;

                // El mismo ProductId no debe venir
                // dos veces en la petición.
                var duplicatedProductIds =
                    request.Products
                        .GroupBy(product =>
                            product.ProductId)
                        .Any(group =>
                            group.Count() > 1);

                if (duplicatedProductIds)
                    return 0;

                // No permitir posiciones repetidas.
                var duplicatedSortOrders =
                    request.Products
                        .GroupBy(product =>
                            product.SortOrder)
                        .Any(group =>
                            group.Count() > 1);

                if (duplicatedSortOrders)
                    return 0;

                // Obtener todos los productos enviados
                // en una única consulta.
                var productIds =
                    request.Products
                        .Select(product =>
                            product.ProductId)
                        .ToList();

                var products = (
                    await _productRepository
                        .GetByIds(productIds)
                ).ToList();

                // Algún ProductId enviado no existe.
                if (products.Count !=
                    productIds.Distinct().Count())
                    return 0;

                // Una referencia puede repetirse si tiene
                // distinta unidad de medida.
                //
                // Lo que NO puede repetirse es:
                // reference + unit_of_measure.
                var duplicatedReferenceAndUnit =
                    products
                        .GroupBy(product => new
                        {
                            Reference =
                                product.reference
                                    .Trim()
                                    .ToUpperInvariant(),

                            UnitOfMeasure =
                                product.unit_of_measure
                                    .Trim()
                                    .ToUpperInvariant()
                        })
                        .Any(group =>
                            group.Count() > 1);

                if (duplicatedReferenceAndUnit)
                    return 0;

                var section = new Section
                {
                    section_name = sectionName,
                    is_active = true
                };

                return await _solutionCenterRepository
                    .CreateSectionConfiguration(
                        solutionCenterId,
                        section,
                        request.Products,
                        request.CreatedBy.Trim());
            }
            catch
            {
                throw;
            }
        }

        public async Task<PagedDto<SolutionCenterListDto>?>
    GetPagedSolutionCenters(
        string role,
        int page,
        int take)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(role))
                    return null;

                if (page <= 0)
                    page = 1;

                if (take <= 0)
                    take = 10;

                if (take > 100)
                    take = 100;

                var normalizedRole = role
                    .Trim()
                    .ToUpperInvariant();

                long? solutionCenterTypeId;

                switch (normalizedRole)
                {
                    case "ALMACEN":

                        // 1 = Bodega
                        // Almacén solo puede visualizar bodegas.
                        solutionCenterTypeId = 1;
                        break;

                    case "COSTOS":

                        // Puede visualizar bodegas
                        // y puntos de venta.
                        solutionCenterTypeId = null;
                        break;

                    case "CONTROL INTERNO":

                        // Puede visualizar bodegas
                        // y puntos de venta.
                        solutionCenterTypeId = null;
                        break;

                    case "ADMINISTRADOR":

                        // Puede visualizar bodegas
                        // y puntos de venta.
                        solutionCenterTypeId = null;
                        break;

                    default:
                        return null;
                }

                return await _solutionCenterRepository
                    .GetPagedSolutionCenters(
                        page,
                        take,
                        solutionCenterTypeId);
            }
            catch
            {
                throw;
            }
        }

        public async Task<SolutionCenterDetailDto?>
    GetSolutionCenterById(
        long solutionCenterId)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return null;

                return await _solutionCenterRepository
                    .GetSolutionCenterById(
                        solutionCenterId);
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> UpdateSolutionCenterStatus(
    long solutionCenterId,
    bool isActive)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return false;

                var solutionCenterExists =
                    await _solutionCenterRepository
                        .SolutionCenterExists(solutionCenterId);

                if (!solutionCenterExists)
                    return false;

                return await _solutionCenterRepository
                    .UpdateSolutionCenterStatus(
                        solutionCenterId,
                        isActive);
            }
            catch
            {
                throw;
            }
        }
        public async Task<bool> UpdateSectionStatus(
    long sectionId,
    bool isActive)
        {
            try
            {
                if (sectionId <= 0)
                    return false;

                return await _solutionCenterRepository
                    .UpdateSectionStatus(
                        sectionId,
                        isActive);
            }
            catch
            {
                throw;
            }
        }
        public async Task<long> AddProductToSection(
    long solutionCenterId,
    long sectionId,
    AddSectionProductDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return 0;

                if (sectionId <= 0)
                    return 0;

                if (request is null)
                    return 0;

                if (request.ProductId <= 0)
                    return 0;

                if (request.Position <= 0)
                    return 0;

                if (string.IsNullOrWhiteSpace(
                    request.CreatedBy))
                    return 0;

                // El centro debe existir.
                var solutionCenterExists =
                    await _solutionCenterRepository
                        .SolutionCenterExists(
                            solutionCenterId);

                if (!solutionCenterExists)
                    return 0;

                // La sección debe pertenecer al centro.
                //
                // IMPORTANTE:
                // NO validamos is_active.
                var sectionBelongs =
                    await _solutionCenterRepository
                        .SectionBelongsToSolutionCenter(
                            solutionCenterId,
                            sectionId);

                if (!sectionBelongs)
                    return 0;

                // Producto que se desea agregar.
                var product =
                    await _productRepository
                        .GetProductById(
                            request.ProductId);

                if (product is null)
                    return 0;

                // Obtener productos actuales del pasillo.
                var sectionProducts = (
                    await _solutionCenterRepository
                        .GetSectionProducts(
                            solutionCenterId,
                            sectionId)
                ).ToList();

                // Validar product_name + unit_of_measure.
                var duplicateExists =
                    sectionProducts.Any(existing =>
                        string.Equals(
                            existing.product_name.Trim(),
                            product.product_name.Trim(),
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        string.Equals(
                            existing.unit_of_measure.Trim(),
                            product.unit_of_measure.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (duplicateExists)
                    return 0;

                // Si hay 10 productos:
                //
                // posiciones válidas:
                // 1 ... 11
                //
                // 11 significa agregar al final.
                var maxPosition =
                    sectionProducts.Count + 1;

                if (request.Position > maxPosition)
                    return 0;

                return await _solutionCenterRepository
                    .AddProductToSection(
                        solutionCenterId,
                        sectionId,
                        request.ProductId,
                        request.Position,
                        request.CreatedBy.Trim());
            }
            catch
            {
                throw;
            }
        }
        public async Task<bool> UpdateProductOrder(
    long solutionCenterId,
    long sectionId,
    long solutionCenterProductId,
    int newPosition)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return false;

                if (sectionId <= 0)
                    return false;

                if (solutionCenterProductId <= 0)
                    return false;

                if (newPosition <= 0)
                    return false;

                var solutionCenterExists =
                    await _solutionCenterRepository
                        .SolutionCenterExists(
                            solutionCenterId);

                if (!solutionCenterExists)
                    return false;

                var sectionBelongs =
                    await _solutionCenterRepository
                        .SectionBelongsToSolutionCenter(
                            solutionCenterId,
                            sectionId);

                if (!sectionBelongs)
                    return false;

                // IMPORTANTE:
                // No validar is_active.
                // Una sección inactiva puede seguir configurándose.

                return await _solutionCenterRepository
                    .UpdateProductOrder(
                        solutionCenterId,
                        sectionId,
                        solutionCenterProductId,
                        newPosition);
            }
            catch
            {
                throw;
            }
        }
        public async Task<bool> DeleteProductFromSection(
    long solutionCenterId,
    long sectionId,
    long solutionCenterProductId)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return false;

                if (sectionId <= 0)
                    return false;

                if (solutionCenterProductId <= 0)
                    return false;

                var solutionCenterExists =
                    await _solutionCenterRepository
                        .SolutionCenterExists(
                            solutionCenterId);

                if (!solutionCenterExists)
                    return false;

                var sectionBelongs =
                    await _solutionCenterRepository
                        .SectionBelongsToSolutionCenter(
                            solutionCenterId,
                            sectionId);

                if (!sectionBelongs)
                    return false;

                // IMPORTANTE:
                // No validamos is_active.
                // Una sección inactiva sigue siendo configurable.

                return await _solutionCenterRepository
                    .DeleteProductFromSection(
                        solutionCenterId,
                        sectionId,
                        solutionCenterProductId);
            }
            catch
            {
                throw;
            }
        }
        public async Task<bool> UpdateSolutionCenter(
    long solutionCenterId,
    UpdateSolutionCenterDto request)
        {
            try
            {
                if (solutionCenterId <= 0)
                    return false;

                if (request is null)
                    return false;

                if (string.IsNullOrWhiteSpace(
                    request.SolutionCenterCode))
                    return false;

                if (string.IsNullOrWhiteSpace(
                    request.SolutionCenterName))
                    return false;

                // Validar que el registro exista.
                var solutionCenterExists =
                    await _solutionCenterRepository
                        .SolutionCenterExists(
                            solutionCenterId);

                if (!solutionCenterExists)
                    return false;

                // Normalizar código.
                var code = request.SolutionCenterCode
                    .Trim()
                    .ToUpperInvariant();

                // Normalizar nombre.
                var name = request.SolutionCenterName
                    .Trim();

                // Validar que ningún OTRO centro
                // tenga el mismo código.
                var codeExists =
                    await _solutionCenterRepository
                        .SolutionCenterCodeExists(
                            code,
                            solutionCenterId);

                if (codeExists)
                    return false;

                // Validar que ningún OTRO centro
                // tenga el mismo nombre.
                var nameExists =
                    await _solutionCenterRepository
                        .SolutionCenterNameExists(
                            name,
                            solutionCenterId);

                if (nameExists)
                    return false;

                return await _solutionCenterRepository
                    .UpdateSolutionCenter(
                        solutionCenterId,
                        code,
                        name);
            }
            catch
            {
                throw;
            }
        }
    }
}