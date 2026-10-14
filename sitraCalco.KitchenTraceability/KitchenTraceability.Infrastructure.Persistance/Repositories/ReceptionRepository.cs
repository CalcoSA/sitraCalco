using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ReceptionRepository : Repository<Reception>, IReceptionRepository
    {
        private readonly SitraCalcoContext _context;

        public ReceptionRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Método para obtener un registro de la tabla reception por Id.
        /// </summary>
        /// <param name="receptionId"> Type: long - Id del registro a buscar. </param>
        /// <returns> Type: Reception - Entidad con la información solicitada </returns>
        public async Task<Reception?> GetByReceptionId(long receptionId)
        {
            return await _context.Receptions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.reception_id == receptionId);
        }

        /// <summary>
        /// Método para obtener el detalle completo de un registro de la tabla reception por Id.
        /// </summary>
        /// <param name="receptionId"> Type: long - Id del registro a buscar. </param>
        /// <returns> Type: Reception - Entidad con la información solicitada </returns>
        public async Task<Reception?> GetReceptionDetailById(long receptionId)
        {
            return await _context.Receptions
                .AsNoTracking()
                .Include(x => x.receptionType)
                .Include(x => x.supplier)
                .Include(x => x.receptionTransportEvaluation)
                .Include(x => x.conformities)
                    .ThenInclude(x => x.receptionType)
                .Include(x => x.receptionLots)
                    .ThenInclude(x => x.receptionVisualInspection)
                .Include(x => x.receptionLots)
                    .ThenInclude(x => x.receptionTemperatureMeasurements)
                .Include(x => x.receptionLots)
                    .ThenInclude(x => x.conformities)
                        .ThenInclude(x => x.receptionType)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.reception_id == receptionId);
        }

        /// <summary>
        /// Método para crear un registro en la tabla reception
        /// </summary>
        /// <param name="reception"> Type: Reception - Información que se desea registrar. </param>
        /// <returns> Type: Reception - Recepción creada con Id y número generado. </returns>
        public async Task<Reception> CreateReception(Reception reception)
        {
            const int maxAttempts = 5;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    reception.reception_number = await GetNextReceptionNumber(reception.reception_date);

                    await _context.Receptions.AddAsync(reception);
                    await _context.SaveChangesAsync();

                    return reception;
                }
                catch (DbUpdateException ex) when (IsDuplicateReceptionNumber(ex))
                {
                    _context.ChangeTracker.Clear();

                    reception.reception_id = 0;

                    if (reception.receptionTransportEvaluation is not null)
                    {
                        reception.receptionTransportEvaluation.transport_evaluation_id = 0;
                        reception.receptionTransportEvaluation.reception_id = 0;
                    }

                    if (attempt == maxAttempts)
                    {
                        throw new InvalidOperationException("No fue posible generar el consecutivo de la recepción.");
                    }
                }
            }

            throw new InvalidOperationException("No fue posible crear la recepción.");
        }

        /// <summary>
        /// Método para obtener el siguiente consecutivo diario de la tabla reception
        /// </summary>
        /// <param name="date"> Type: DateTime - Fecha utilizada para generar el consecutivo </param>
        /// <returns> Type: string - Número de recepción generado </returns>
        private async Task<string> GetNextReceptionNumber(DateTime date)
        {
            var prefix = date.ToString("yyyyMMdd");

            var lastReceptionNumber =
                await _context.Receptions
                    .AsNoTracking()
                    .Where(x => x.reception_number.StartsWith(prefix))
                    .OrderByDescending(x => x.reception_number)
                    .Select(x => x.reception_number)
                    .FirstOrDefaultAsync();

            var nextSequence = 1;

            if (!string.IsNullOrWhiteSpace(lastReceptionNumber))
            {
                var sequence = lastReceptionNumber[^2..];

                if (int.TryParse(sequence, out var lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            if (nextSequence > 99)
            {
                throw new InvalidOperationException("Se alcanzó el máximo de 99 recepciones para el día.");
            }

            return $"{prefix}{nextSequence:D2}";
        }

        /// <summary>
        /// Método para determinar si el error corresponde a un número de recepción duplicado.
        /// </summary>
        /// <param name="exception"> Type: DbUpdateException - Excepción generada por EF. </param>
        /// <returns> Type: bool - True si corresponde al índice único del número de recepción </returns>
        private static bool IsDuplicateReceptionNumber(DbUpdateException exception)
        {
            return exception.InnerException
                    is MySqlException mysqlException
                && mysqlException.Number == 1062
                && mysqlException.Message.Contains(
                    "uk_ktr_reception_number",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}