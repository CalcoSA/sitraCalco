using KitchenTraceability.Application.Interfaces;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using KitchenTraceability.Domain.Dtos;
using AutoMapper;

namespace KitchenTraceability.Application.Services
{
    public class ReceptionLotApplication : IReceptionLotApplication
    {
        private const string ApprovedStatus = "APPROVED";
        private const string PendingApprovalStatus = "PENDING_APPROVAL";
        private const string RejectedStatus = "REJECTED";
        private const string IdealTemperatureStatus = "IDEAL";
        private const string ConditionalTemperatureStatus = "CONDITIONAL";
        private const string RejectedTemperatureStatus = "REJECTED";
        private const string NonconformityFormCode = "CSL-F-PPP-54";
        private const string ShelfLifeReason = "SHELF_LIFE";
        private const string TemperatureReason = "TEMPERATURE";
        private const string VisualInspectionReason = "VISUAL_INSPECTION";
        private const string OpenStatus = "OPEN";

        private readonly IProductTemperatureConfigurationRepository _productTemperatureConfigurationRepository;
        private readonly IProductReceptionConfigurationRepository _productReceptionConfigurationRepository;
        private readonly IReceptionTemperatureMeasurementRepository _temperatureRepository;
        private readonly IReceptionVisualInspectionRepository _visualInspectionRepository;
        private readonly IReceptionTypeRepository _receptionTypeRepository;
        private readonly IReceptionLotRepository _receptionLotRepository;
        private readonly IConformityRepository _conformityRepository;
        private readonly IReceptionRepository _receptionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public ReceptionLotApplication(IProductTemperatureConfigurationRepository productTemperatureConfigurationRepository,
            IProductReceptionConfigurationRepository productReceptionConfigurationRepository,
            IReceptionTemperatureMeasurementRepository temperatureMeasurementRepository,
            IReceptionVisualInspectionRepository visualInspectionRepository,
            IReceptionTypeRepository receptionTypeRepository,
            IReceptionLotRepository receptionLotRepository,
            IConformityRepository conformityRepository,
            IReceptionRepository receptionRepository,
            IProductRepository productRepository,
            IMapper mapper)
        {
            _productTemperatureConfigurationRepository = productTemperatureConfigurationRepository;
            _productReceptionConfigurationRepository = productReceptionConfigurationRepository;
            _temperatureRepository = temperatureMeasurementRepository;
            _visualInspectionRepository = visualInspectionRepository;
            _receptionTypeRepository = receptionTypeRepository;
            _receptionLotRepository = receptionLotRepository;
            _conformityRepository = conformityRepository;
            _receptionRepository = receptionRepository;
            _productRepository = productRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para crear un lote asociado a una recepción
        /// </summary>
        /// <param name="request"> Type: CreateReceptionLotDto - Información a registrar </param>
        /// <param name="user"> Type: string - Usuario autenticado que realiza el registro </param>
        /// <returns> Type: CreateReceptionLotResultDto - Resultado de la creación </returns>
        public async Task<CreateReceptionLotResultDto> CreateLot(CreateReceptionLotDto request, string user)
        {
            try
            {
                var reception = await _receptionRepository.GetByReceptionId(request.reception_id);

                if (reception is null)
                {
                    throw new KeyNotFoundException("La recepción no existe.");
                }

                var product = await _productRepository.GetProductById(request.product_id);

                if (product is null)
                {
                    throw new KeyNotFoundException("El producto no existe.");
                }

                var productConfiguration = await _productReceptionConfigurationRepository.GetByProductId(request.product_id);

                if (productConfiguration is null)
                {
                    throw new InvalidOperationException("El producto no tiene una configuración de recepción.");
                }

                if (productConfiguration.requires_guide == true && string.IsNullOrWhiteSpace(request.guide_number))
                {
                    throw new InvalidOperationException("El producto requiere número de guía.");
                }

                var lotStatus = ApprovedStatus;
                bool? shelfLife = null;
                string? shelfLifeComments = null;

                if (productConfiguration.requires_shelf_life == true)
                {
                    if (!request.expiration_date.HasValue)
                    {
                        throw new InvalidOperationException("El producto requiere fecha de vencimiento.");
                    }

                    if (!productConfiguration.minimum_shelf_life_days.HasValue)
                    {
                        throw new InvalidOperationException("El producto requiere validar vida útil pero no tiene configurado el mínimo de días.");
                    }

                    var receptionDate = DateOnly.FromDateTime(reception.reception_date);
                    var remainingDays = request.expiration_date.Value.DayNumber - receptionDate.DayNumber;

                    shelfLife = remainingDays >= productConfiguration.minimum_shelf_life_days.Value;

                    if (shelfLife == false)
                    {
                        ValidateShelfLifeFailure(request);

                        lotStatus = request.lot_status!;
                        shelfLifeComments = request.shelf_life_comments;
                    }
                }

                var receptionLot = _mapper.Map<ReceptionLot>(request);

                receptionLot.reception_id = reception.reception_id;
                receptionLot.product_reference = product.product_reference;
                receptionLot.lot_size_unit = product.unit_of_measure;
                receptionLot.unit_of_measure = product.unit_of_measure;
                receptionLot.shelf_life = shelfLife;
                receptionLot.shelf_life_comments = shelfLifeComments;
                receptionLot.lot_status = lotStatus;
                receptionLot.created_at = GetColombiaDateTime();

                await _receptionLotRepository.Add(receptionLot);

                Conformity? conformity = null;
                ReceptionType? nonconformityForm = null;

                if (shelfLife == false && lotStatus == RejectedStatus)
                {
                    nonconformityForm = await GetNonconformityForm();

                    var nonconformityNumber = await GetNextNonconformityNumber();

                    conformity = _mapper.Map<Conformity>(request);

                    conformity.nonconformity_number = nonconformityNumber;
                    conformity.reception_id = reception.reception_id;
                    conformity.reception_lot_id = receptionLot.reception_lot_id;
                    conformity.reception_type_id = nonconformityForm.reception_type_id;
                    conformity.reason_type = ShelfLifeReason;
                    conformity.observations = request.shelf_life_comments!;
                    conformity.status = OpenStatus;
                    conformity.created_by = user;
                    conformity.created_at = GetColombiaDateTime();

                    await _conformityRepository.Add(conformity);
                }

                var response = _mapper.Map<CreateReceptionLotResultDto>(receptionLot);

                response.requires_temperature = productConfiguration.requires_temperature == true;
                response.requires_visual_inspection = productConfiguration.requires_visual_inspection == true;
                response.nonconformity_id = conformity?.nonconformity_id;
                response.nonconformity_number = conformity?.nonconformity_number;
                response.nonconformity_form_code = conformity is null ? null : nonconformityForm?.form_code;

                return response;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que valida la información requerida cuando el lote no cumple la vida útil mínima configurada.
        /// </summary>
        /// <param name="request"> Type: CreateReceptionLotDto - Información recibida </param>
        private static void ValidateShelfLifeFailure(CreateReceptionLotDto request)
        {
            if (string.IsNullOrWhiteSpace(request.lot_status))
            {
                throw new InvalidOperationException("Debe indicar si el lote queda pendiente de aprobación o rechazado.");
            }

            if (request.lot_status != PendingApprovalStatus && request.lot_status != RejectedStatus)
            {
                throw new InvalidOperationException("El estado solo puede ser pendiente por aprobación o rechazado.");
            }

            if (string.IsNullOrWhiteSpace(request.shelf_life_comments))
            {
                throw new InvalidOperationException("Debe ingresar una observación cuando el lote no cumple la vida útil.");
            }
        }

        /// <summary>
        /// Método que obtiene el formato utilizado para registrar las no conformidades.
        /// </summary>
        private async Task<ReceptionType> GetNonconformityForm()
        {
            var receptionType = await _receptionTypeRepository.GetByFormCode(NonconformityFormCode);

            if (receptionType is null)
            {
                throw new InvalidOperationException($"No se encontró el formato {NonconformityFormCode} para registrar la no conformidad.");
            }

            return receptionType;
        }

        /// <summary>
        /// Método que obtiene el siguiente número consecutivo para un registro de no conformidad.
        /// </summary>
        /// <returns> Type: string - Número de no conformidad generado </returns>
        private async Task<string> GetNextNonconformityNumber()
        {
            var lastNumber = await _conformityRepository.GetLastNonconformityNumber();

            if (string.IsNullOrWhiteSpace(lastNumber))
            {
                return "LOG-0001";
            }

            var number = lastNumber.Replace("LOG-", string.Empty);

            if (!int.TryParse(number, out var lastValue))
            {
                throw new InvalidOperationException("El último número de no conformidad registrado no tiene un formato válido.");
            }

            var nextValue = lastValue + 1;

            return $"LOG-{nextValue:D4}";
        }

        /// <summary>
        /// Método que recibe la solicitud del controlador para registrar las mediciones de temperatura y la inspección visual de un lote.
        /// </summary>
        /// <param name="request"> Type: CreateReceptionLotEvaluationDto - Información de la evaluación </param>
        /// <param name="user"> Type: string - Usuario autenticado que realiza la evaluación </param>
        /// <returns> Type: CreateReceptionLotEvaluationResultDto - Resultado de la evaluación </returns>
        public async Task<CreateReceptionLotEvaluationResultDto> Create(CreateReceptionLotEvaluationDto request, string user)
        {
            try
            {
                var receptionLot = await _receptionLotRepository.GetByReceptionLotId(request.reception_lot_id);

                if (receptionLot is null)
                {
                    throw new KeyNotFoundException("El lote de recepción no existe.");
                }

                if (receptionLot.lot_status == RejectedStatus)
                {
                    throw new InvalidOperationException("El lote ya se encuentra rechazado y no puede continuar con la evaluación.");
                }

                var product = await _productRepository.GetByReference(receptionLot.product_reference, receptionLot.unit_of_measure);

                if (product is null)
                {
                    throw new InvalidOperationException("No se encontró el producto asociado al lote.");
                }

                var receptionConfiguration = await _productReceptionConfigurationRepository.GetByProductId(product.product_id);

                if (receptionConfiguration is null)
                {
                    throw new InvalidOperationException("El producto no tiene una configuración de recepción.");
                }

                var temperatureResults = new List<ReceptionTemperatureMeasurement>();

                ReceptionVisualInspection? visualInspection = null;
                Conformity? conformity = null;
                ReceptionType? nonconformityForm = null;

                if (receptionConfiguration.requires_temperature == true)
                {
                    if (request.temperature_measurements is null || request.temperature_measurements.Count() != 3)
                    {
                        throw new InvalidOperationException("El producto requiere las tres mediciones de temperatura: al inicio, a la mitad y al final.");
                    }

                    var existingMeasurements = await _temperatureRepository.GetByReceptionLotId(receptionLot.reception_lot_id);

                    if (existingMeasurements.Any())
                    {
                        throw new InvalidOperationException("El lote ya tiene mediciones de temperatura registradas.");
                    }

                    var temperatureConfiguration = await _productTemperatureConfigurationRepository.GetByProductId(product.product_id);

                    if (temperatureConfiguration is null)
                    {
                        throw new InvalidOperationException("El producto requiere temperatura pero no tiene una configuración de temperatura.");
                    }

                    ValidateMeasurementPoints(request.temperature_measurements);

                    foreach (var measurementRequest in request.temperature_measurements)
                    {
                        var temperatureStatus = GetTemperatureStatus(measurementRequest.temperature_celsius, temperatureConfiguration);

                        if ((temperatureStatus == ConditionalTemperatureStatus || temperatureStatus == RejectedTemperatureStatus) && string.IsNullOrWhiteSpace(measurementRequest.comments))
                        {
                            throw new InvalidOperationException($"Debe ingresar una observación para la temperatura {temperatureStatus} del punto {measurementRequest.measurement_point}.");
                        }

                        var measurement = _mapper.Map<ReceptionTemperatureMeasurement>(measurementRequest);

                        measurement.reception_lot_id = receptionLot.reception_lot_id;
                        measurement.temperature_status = temperatureStatus;
                        measurement.measured_by = user;
                        measurement.measured_at = GetColombiaDateTime();

                        await _temperatureRepository.Add(measurement);

                        temperatureResults.Add(measurement);
                    }

                    var hasRejectedTemperature = temperatureResults.Any(x => x.temperature_status == RejectedTemperatureStatus);
                    var hasConditionalTemperature = temperatureResults.Any(x => x.temperature_status == ConditionalTemperatureStatus);

                    if (hasRejectedTemperature)
                    {
                        receptionLot.lot_status = RejectedStatus;

                        await _receptionLotRepository.Update(receptionLot);

                        nonconformityForm = await GetNonconformityForm();

                        var nonconformityNumber = await GetNextNonconformityNumber();

                        var rejectedMeasurements =
                            temperatureResults
                                .Where(x => x.temperature_status == RejectedTemperatureStatus)
                                .ToList();

                        conformity =
                            new Conformity
                            {
                                nonconformity_number = nonconformityNumber,
                                reception_id = receptionLot.reception_id,
                                reception_lot_id = receptionLot.reception_lot_id,
                                reception_type_id = nonconformityForm.reception_type_id,
                                reason_type = TemperatureReason,
                                document_type = request.document_type,
                                document_number = request.document_number,
                                observations = string.Join(" | ", rejectedMeasurements.Select(x => $"{x.measurement_point}: {x.temperature_celsius} °C - {x.comments}")),
                                status = OpenStatus,
                                created_by = user,
                                created_at = GetColombiaDateTime()
                            };

                        await _conformityRepository.Add(conformity);

                        return BuildResponse(receptionLot, temperatureResults, null, conformity, nonconformityForm);
                    }

                    if (hasConditionalTemperature && receptionLot.lot_status == ApprovedStatus)
                    {
                        receptionLot.lot_status = PendingApprovalStatus;

                        await _receptionLotRepository.Update(receptionLot);
                    }
                }
                else
                {
                    if (request.temperature_measurements is not null && request.temperature_measurements.Any())
                    {
                        throw new InvalidOperationException("El producto no requiere evaluación de temperatura.");
                    }
                }

                if (receptionConfiguration.requires_visual_inspection == true)
                {
                    if (request.visual_inspection is null)
                    {
                        throw new InvalidOperationException("El producto requiere inspección visual.");
                    }

                    var existingVisualInspection = await _visualInspectionRepository.GetByReceptionLotId(receptionLot.reception_lot_id);

                    if (existingVisualInspection is not null)
                    {
                        throw new InvalidOperationException("El lote ya tiene una inspección visual registrada.");
                    }

                    var inspectedUnits = receptionLot.unit_count ?? request.visual_inspection.inspected_units;

                    if (!inspectedUnits.HasValue || inspectedUnits.Value <= 0)
                    {
                        throw new InvalidOperationException("Debe indicar la cantidad de unidades inspeccionadas.");
                    }

                    ValidateVisualInspection(inspectedUnits.Value, request.visual_inspection);

                    visualInspection = _mapper.Map<ReceptionVisualInspection>(request.visual_inspection);

                    visualInspection.reception_lot_id = receptionLot.reception_lot_id;
                    visualInspection.inspected_units = inspectedUnits.Value;
                    visualInspection.evaluated_by = user;
                    visualInspection.evaluated_at = GetColombiaDateTime();

                    await _visualInspectionRepository.Add(visualInspection);

                    if (visualInspection.non_compliant_units > 0)
                    {
                        nonconformityForm = await GetNonconformityForm();

                        var nonconformityNumber = await GetNextNonconformityNumber();

                        conformity =
                            new Conformity
                            {
                                nonconformity_number = nonconformityNumber,
                                reception_id = receptionLot.reception_id,
                                reception_lot_id = receptionLot.reception_lot_id,
                                reception_type_id = nonconformityForm.reception_type_id,
                                reason_type = VisualInspectionReason,
                                quantity = visualInspection.non_compliant_units,
                                document_type = request.document_type,
                                document_number = request.document_number,
                                observations = $"Unidades rechazadas: {visualInspection.non_compliant_units}. {visualInspection.comments}",
                                status = OpenStatus,
                                created_by = user,
                                created_at = GetColombiaDateTime()
                            };

                        await _conformityRepository.Add(conformity);
                    }
                }
                else
                {
                    if (request.visual_inspection is not null)
                    {
                        throw new InvalidOperationException("El producto no requiere inspección visual.");
                    }
                }

                return BuildResponse(receptionLot, temperatureResults, visualInspection, conformity, nonconformityForm);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Exception exception = new("Failed" + ex.InnerException + "\n" + ex.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Método que valida que existan exactamente las tres
        /// mediciones START, MIDDLE y END.
        /// </summary>
        private static void ValidateMeasurementPoints(IEnumerable<CreateReceptionTemperatureMeasurementDto> measurements)
        {
            var points =
                measurements
                    .Select(x => x.measurement_point)
                    .ToList();

            var requiredPoints =
                new[]
                {
                    "START",
                    "MIDDLE",
                    "END"
                };

            if (points.Count != 3 || points.Distinct().Count() != 3 || requiredPoints.Any(x => !points.Contains(x)))
            {
                throw new InvalidOperationException("Debe registrar exactamente una medición al inicio, una a la mitad y una al final.");
            }
        }

        /// <summary>
        /// Método que determina el estado de una temperatura
        /// según la configuración del producto.
        /// </summary>
        private static string GetTemperatureStatus(decimal temperature, ProductTemperatureConfiguration configuration)
        {
            if (temperature >= configuration.ideal_min_temperature && temperature <= configuration.ideal_max_temperature)
            {
                return IdealTemperatureStatus;
            }

            if (IsInsideRange(temperature, configuration.conditional_min_temperature, configuration.conditional_max_temperature))
            {
                return ConditionalTemperatureStatus;
            }

            if (IsInsideRange(temperature, configuration.rejection_min_temperature, configuration.rejection_max_temperature))
            {
                return RejectedTemperatureStatus;
            }

            throw new InvalidOperationException($"La temperatura {temperature} °C no se encuentra dentro de ninguno de los rangos configurados para el producto.");
        }

        /// <summary>
        /// Método que valida si un valor pertenece a un rango
        /// cuyos límites pueden ser nulos.
        /// </summary>
        private static bool IsInsideRange(decimal temperature, decimal? minimum, decimal? maximum)
        {
            if (!minimum.HasValue && !maximum.HasValue)
            {
                return false;
            }

            if (minimum.HasValue && temperature < minimum.Value)
            {
                return false;
            }

            if (maximum.HasValue && temperature > maximum.Value)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Método que valida las cantidades de la inspección visual.
        /// </summary>
        private static void ValidateVisualInspection(decimal inspectedUnits, CreateReceptionVisualInspectionDto request)
        {
            if (request.compliant_units < 0 || request.non_compliant_units < 0)
            {
                throw new InvalidOperationException("Las cantidades de la inspección visual no pueden ser negativas.");
            }

            if (request.compliant_units + request.non_compliant_units != inspectedUnits)
            {
                throw new InvalidOperationException("La suma de unidades conformes y no conformes debe ser igual a las unidades inspeccionadas.");
            }

            if (request.non_compliant_units > 0 && string.IsNullOrWhiteSpace(request.comments))
            {
                throw new InvalidOperationException("Debe ingresar una observación cuando existen unidades no conformes.");
            }
        }

        /// <summary>
        /// Método que construye la respuesta de la evaluación del lote.
        /// </summary>
        private CreateReceptionLotEvaluationResultDto BuildResponse(ReceptionLot receptionLot, IEnumerable<ReceptionTemperatureMeasurement> temperatureMeasurements, ReceptionVisualInspection? visualInspection, Conformity? conformity, ReceptionType? nonconformityForm)
        {
            return new CreateReceptionLotEvaluationResultDto
            {
                reception_lot_id = receptionLot.reception_lot_id,
                lot_status = receptionLot.lot_status,
                temperature_measurements = _mapper.Map<IEnumerable<ReceptionTemperatureMeasurementResultDto>>(temperatureMeasurements),
                visual_inspection = visualInspection is null ? null : _mapper.Map<ReceptionVisualInspectionResultDto>(visualInspection),
                nonconformity_id = conformity?.nonconformity_id,
                nonconformity_number = conformity?.nonconformity_number,
                nonconformity_form_code = conformity is null ? null : nonconformityForm?.form_code,
                nonconformity_reason = conformity?.reason_type
            };
        }

        /// <summary>
        /// Método para obtener la fecha y hora actual de Colombia.
        /// </summary>
        /// <returns>Type: DateTime - Fecha y hora actual de Colombia.</returns>
        private static DateTime GetColombiaDateTime()
        {
            var colombiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, colombiaTimeZone);
        }
    }
}