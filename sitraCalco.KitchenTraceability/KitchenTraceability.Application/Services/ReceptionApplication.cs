using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;

namespace KitchenTraceability.Application.Services
{
    public class ReceptionApplication : IReceptionApplication
    {
        private readonly IReceptionRepository _receptionRepository;
        private readonly IProductRepository _productRepository;

        public ReceptionApplication(IReceptionRepository receptionRepository,
            IProductRepository productRepository)
        {
            _receptionRepository = receptionRepository;
            _productRepository = productRepository;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para obtener el detalle completo de una recepción.
        /// </summary>
        /// <param name="receptionId"> Type: long - Id de la recepción </param>
        /// <returns> Type: ReceptionDetailDto - Información completa de la recepción </returns>
        public async Task<ReceptionDetailDto?> GetById(long receptionId)
        {
            string BuildProductKey(string reference, string unitOfMeasure)
            {
                return string.Join("|", reference.Trim().ToUpperInvariant(), unitOfMeasure.Trim().ToUpperInvariant());
            }

            ConformityDetailDto MapConformity(Conformity conformity)
            {
                return new ConformityDetailDto
                {
                    nonconformity_id = conformity.nonconformity_id,
                    nonconformity_number = conformity.nonconformity_number,
                    reception_type_name = conformity.receptionType.name,
                    reception_type_form_code = conformity.receptionType.form_code,
                    reason_type = conformity.reason_type,
                    quantity = conformity.quantity,
                    document_type = conformity.document_type,
                    document_number = conformity.document_number,
                    observations = conformity.observations,
                    status = conformity.status
                };
            }

            try
            {
                var reception = await _receptionRepository.GetReceptionDetailById(receptionId);

                if (reception is null)
                {
                    return null;
                }

                var references = reception.receptionLots.Select(x => x.product_reference).Distinct().ToList();
                var products = await _productRepository.GetByReferences(references);

                var productDictionary =
                    products
                        .GroupBy(x => BuildProductKey( x.product_reference, x.unit_of_measure))
                        .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

                var lots =
                    reception.receptionLots
                        .OrderBy(x => x.reception_lot_id)
                        .Select(lot =>
                        {
                            productDictionary.TryGetValue(BuildProductKey(lot.product_reference, lot.unit_of_measure), out var product);

                            return new ReceptionLotDetailDto
                            {
                                reception_lot_id = lot.reception_lot_id,
                                lot_number = lot.lot_number,
                                product_reference = lot.product_reference,
                                product_name = product?.product_name,
                                unit_of_measure = lot.unit_of_measure,
                                guide_number = lot.guide_number,
                                expiration_date = lot.expiration_date,
                                lot_size = lot.lot_size,
                                lot_size_unit = lot.lot_size_unit,
                                unit_count = lot.unit_count,
                                shelf_life = lot.shelf_life,
                                shelf_life_comments = lot.shelf_life_comments,
                                lot_status = lot.lot_status,

                                visual_inspection =
                                    lot.receptionVisualInspection is null
                                        ? null
                                        : new ReceptionVisualInspectionDetailDto
                                        {
                                            compliant_units = lot.receptionVisualInspection.compliant_units,
                                            non_compliant_units = lot.receptionVisualInspection.non_compliant_units,
                                            comments = lot.receptionVisualInspection.comments
                                        },

                                temperature_measurements =
                                    lot.receptionTemperatureMeasurements
                                        .Select(x =>
                                            new ReceptionTemperatureMeasurementResultDto
                                            {
                                                temperature_measurement_id = x.temperature_measurement_id,
                                                measurement_point = x.measurement_point,
                                                temperature_celsius = x.temperature_celsius,
                                                temperature_status = x.temperature_status,
                                                comments = x.comments
                                            })
                                        .ToList(),

                                nonconformities =
                                    lot.conformities
                                        .OrderBy(x => x.nonconformity_id)
                                        .Select(x => MapConformity(x))
                                        .ToList()
                            };
                        })
                        .ToList();

                var receptionNonconformities =
                    reception.conformities
                        .Where(x => x.reception_lot_id == null)
                        .OrderBy(x => x.nonconformity_id)
                        .Select(x => MapConformity(x))
                        .ToList();

                return new ReceptionDetailDto
                {
                    reception_id = reception.reception_id,
                    reception_number = reception.reception_number,
                    reception_type_name = reception.receptionType.name,
                    reception_type_form_code = reception.receptionType.form_code,
                    supplier_code = reception.supplier.supplier_code,
                    supplier_name = reception.supplier.name,
                    reception_date = reception.reception_date,
                    status = reception.status,
                    transport_is_compliant = reception.receptionTransportEvaluation?.is_compliant,
                    transport_comments = reception.receptionTransportEvaluation?.comments,
                    lots = lots,
                    nonconformities = receptionNonconformities
                };
            }
            catch
            {
                throw;
            }
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