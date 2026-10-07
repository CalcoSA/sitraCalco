using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Responses;
using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;
using AutoMapper;

namespace KitchenTraceability.Application.Services
{
    public class ReceptionTypeApplication : IReceptionTypeApplication
    {
        private readonly IReceptionTypeRepository _receptionTypeRepository;
        private readonly IMapper _mapper;

        public ReceptionTypeApplication(IReceptionTypeRepository receptionTypeRepository,
            IMapper mapper)
        {
            _receptionTypeRepository = receptionTypeRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener los tipos de recepción.
        /// </summary>
        /// <param name="page">Type: int - Número de página.</param>
        /// <param name="take">Type: int - Cantidad de registros por página.</param>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o código de formulario.</param>
        /// <returns>Type: Page - Lista paginada con la información solicitada.</returns>
        public async Task<Page<ReceptionTypeDto>> GetAll(int page, int take, string? search = null)
        {
            try
            {
                var receptionTypes = await _receptionTypeRepository.GetPaged(page, take, search);

                return new Page<ReceptionTypeDto>
                {
                    Items = _mapper.Map<IEnumerable<ReceptionTypeDto>>(receptionTypes.Items),
                    PageNumber = receptionTypes.PageNumber,
                    PageSize = receptionTypes.PageSize,
                    TotalRecords = receptionTypes.TotalRecords,
                    TotalPages = receptionTypes.TotalPages
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener los tipos de recepción activos para una lista desplegable.
        /// </summary>
        /// <param name="search">Type: string - Texto opcional para buscar por nombre o código de formulario.</param>
        /// <returns>Type: IEnumerable - Lista de tipos de recepción activos.</returns>
        public async Task<IEnumerable<ReceptionTypeDto>> GetOptions(string? search = null)
        {
            try
            {
                var receptionTypes = await _receptionTypeRepository.GetOptions(search);
                return _mapper.Map<IEnumerable<ReceptionTypeDto>>(receptionTypes);
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener un tipo de recepción por Id.
        /// </summary>
        /// <param name="id">Type: long - Id del tipo de recepción.</param>
        /// <returns>Type: ReceptionTypeDto - Información solicitada.</returns>
        public async Task<ReceptionTypeDto?> GetById(long id)
        {
            try
            {
                var receptionType = await _receptionTypeRepository.GetReceptionTypeById(id);

                if (receptionType is null)
                {
                    return null;
                }

                return _mapper.Map<ReceptionTypeDto>(receptionType);
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para crear un tipo de recepción.
        /// </summary>
        /// <param name="request">Type: CreateReceptionTypeDto - Información a registrar.</param>
        /// <returns>Type: bool - True si fue creado correctamente.</returns>
        public async Task<bool> Create(CreateReceptionTypeDto request)
        {
            try
            {
                var receptionType = _mapper.Map<ReceptionType>(request);
                await _receptionTypeRepository.Add(receptionType);
                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para actualizar un tipo de recepción existente.
        /// </summary>
        /// <param name="id">Type: long - Id del tipo de recepción.</param>
        /// <param name="request">Type: UpdateReceptionTypeDto - Información a actualizar.</param>
        /// <returns>Type: bool - True si fue actualizado; false si no existe.</returns>
        public async Task<bool> Update(long id, UpdateReceptionTypeDto request)
        {
            try
            {
                var receptionType = await _receptionTypeRepository.GetReceptionTypeById(id);

                if (receptionType is null)
                {
                    return false;
                }

                _mapper.Map(request, receptionType);
                await _receptionTypeRepository.Update(receptionType);
                return true;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para eliminar un tipo de recepción por Id.
        /// </summary>
        /// <param name="id">Type: long - Id del tipo de recepción.</param>
        /// <returns>Type: bool - True si fue eliminado; false si no existe.</returns>
        public async Task<bool> Delete(long id)
        {
            try
            {
                var receptionType = await _receptionTypeRepository.GetReceptionTypeById(id);

                if (receptionType is null)
                {
                    return false;
                }

                await _receptionTypeRepository.Delete(receptionType);
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