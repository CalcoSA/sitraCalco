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
    }
}