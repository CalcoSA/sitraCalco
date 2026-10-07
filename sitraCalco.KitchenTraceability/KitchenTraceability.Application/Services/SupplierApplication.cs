using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;
using AutoMapper;

namespace KitchenTraceability.Application.Services
{
    public class SupplierApplication : ISupplierApplication
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly IMapper _mapper;

        public SupplierApplication(ISupplierRepository supplierRepository,
            IMapper mapper)
        {
            _supplierRepository = supplierRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener todos todos los proveedores registrados.
        /// </summary>
        /// <param name="page">Type: int - Número de página a consultar.</param>
        /// <param name="take">Type: int - Cantidad de registros por página.</param>
        /// <returns>Type: Page - Lista paginada con la información solicitada.</returns>
        public async Task<Page<SupplierDto>> GetAll(int page, int take)
        {
            try
            {
                var suppliers = await _supplierRepository.GetAllSupplier(page, take);

                return new Page<SupplierDto>
                {
                    Items = _mapper.Map<IEnumerable<SupplierDto>>(suppliers.Items),
                    PageNumber = suppliers.PageNumber,
                    PageSize = suppliers.PageSize,
                    TotalRecords = suppliers.TotalRecords,
                    TotalPages = suppliers.TotalPages
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener proveedores
        /// disponibles para una lista desplegable.
        /// </summary>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o código.</param>
        /// <returns>Type: IEnumerable - Lista de proveedores encontrados.</returns>
        public async Task<IEnumerable<SupplierDto>> GetOptions(string? search = null)
        {
            try
            {
                var suppliers = await _supplierRepository.GetOptions(search, 20);
                return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener un proveedor por Id.
        /// </summary>
        /// <param name="id">Type: long - Id del proveedor a buscar</param>
        /// <returns>Type: SupplierDto - Dto con la información solicitada</returns>
        public async Task<SupplierDto?> GetById(long id)
        {
            try
            {
                var supplier = await _supplierRepository.GetBySupplierId(id);

                if (supplier is null)
                {
                    return null;
                }

                return _mapper.Map<SupplierDto>(supplier);
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para crear un proveedor.
        /// </summary>
        /// <param name="request">Type: CreateSupplierDto - Información del proveedor a crear.</param>
        /// <returns>Type: bool - True si el proveedor fue creado correctamente.</returns>
        public async Task<bool> Create(CreateSupplierDto request)
        {
            try
            {
                var exists = await _supplierRepository.ExistsByCode(request.supplier_code);

                if (exists)
                {
                    throw new InvalidOperationException("Ya existe un proveedor con el código ingresado.");
                }

                var supplier = _mapper.Map<Supplier>(request);

                await _supplierRepository.Add(supplier);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para actualizar un proveedor existente.
        /// </summary>
        /// <param name="id">Type: long - Id del proveedor.</param>
        /// <param name="request">Type: UpdateSupplierDto - Información del proveedor a actualizar.</param>
        /// <returns>Type: bool - True si el proveedor fue actualizado correctamente; false si no existe.</returns>
        public async Task<bool> Update(long id, UpdateSupplierDto request)
        {
            try
            {
                var supplier = await _supplierRepository.GetBySupplierId(id);

                if (supplier is null)
                {
                    return false;
                }

                var codeExists = await _supplierRepository.ExistsByCode(request.supplier_code, id);

                if (codeExists)
                {
                    throw new InvalidOperationException("Ya existe otro proveedor con el código ingresado.");
                }

                _mapper.Map(request, supplier);

                await _supplierRepository.Update(supplier);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para eliminar un proveedor por Id.
        /// </summary>
        /// <param name="id">Type: int - Id del proveedor a eliminar</param>
        /// <returns>Type: bool - True si el proveedor fue eliminado correctamente</returns>
        public async Task<bool> Delete(long id)
        {
            try
            {
                var supplier = await _supplierRepository.GetBySupplierId(id);

                if (supplier is null)
                {
                    return false;
                }

                await _supplierRepository.Delete(supplier);

                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }
    }
}