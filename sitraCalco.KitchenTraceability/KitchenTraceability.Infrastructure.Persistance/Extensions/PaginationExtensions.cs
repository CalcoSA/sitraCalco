using KitchenTraceability.Domain.Responses;
using Microsoft.EntityFrameworkCore;

namespace KitchenTraceability.Infrastructure.Persistance.Extensions
{
    public static class PaginationExtensions
    {
        /// <summary>
        /// Método genérico para paginar una consulta IQueryable.
        /// </summary>
        /// <typeparam name="T">Type: T - Entidad que se desea paginar.</typeparam>
        /// <param name="query">Type: IQueryable - Consulta que será paginada.</param>
        /// <param name="page">Type: int - Número de página.</param>
        /// <param name="take">Type: int - Cantidad de registros por página.</param>
        /// <returns>Type: Page - Información paginada.</returns>
        public static async Task<Page<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int take)
        {
            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * take)
                .Take(take)
                .ToListAsync();

            return new Page<T>
            {
                Items = items,
                PageNumber = page,
                PageSize = take,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)take)
            };
        }
    }
}