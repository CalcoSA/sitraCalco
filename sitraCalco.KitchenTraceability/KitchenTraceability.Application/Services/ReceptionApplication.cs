using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Services
{
    public class ReceptionApplication : IReceptionApplication
    {
        private readonly IReceptionRepository _receptionRepository;

        public ReceptionApplication(IReceptionRepository receptionRepository)
        {
            _receptionRepository = receptionRepository;
        }

        /// <summary>
        /// Método que recibe la solicutud del controlador para crear una recepción
        /// </summary>
        /// <param name="request"> Type: CreateReceptionDto - Información a registrar.</param>
        /// <param name="user"> Type: string - Usuario autenticado que registra la información </param>
        /// <returns> Type: CreateReceptionResultDto - Id, número y estado de la recepción creada </returns>
        public async Task<CreateReceptionResultDto> Create(CreateReceptionDto request, string user)
        {
            try
            {
                var now = GetCurrentDateTime();

                var receptionStatus = request.is_compliant ? "APPROVED" : "PENDING_APPROVAL";

                var reception = new Reception
                {
                    reception_type_id = request.reception_type_id,
                    supplier_id = request.supplier_id,
                    guide_number = request.guide_number,
                    reception_date = now,
                    operator_user = user,
                    comments = null,
                    status = receptionStatus,
                    created_at = now,

                    receptionTransportEvaluation =
                        new ReceptionTransportEvaluation
                        {
                            is_compliant = request.is_compliant,
                            comments = request.is_compliant ? null : request.comments,
                            evaluated_by = user,
                            evaluated_at = now
                        }
                };

                var createdReception = await _receptionRepository.CreateReception(reception);

                return new CreateReceptionResultDto
                {
                    reception_id = createdReception.reception_id,
                    reception_number = createdReception.reception_number,
                    status = createdReception.status
                };
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método para obtener la fecha y hora actual correspondiente a Colombia.
        /// </summary>
        /// <returns> Type: DateTime - Fecha y hora actual </returns>
        private static DateTime GetCurrentDateTime()
        {
            var timeZoneId = OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota";
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }
    }
}