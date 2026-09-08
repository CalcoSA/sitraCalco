using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistance.Repositories
{
    public class SolutionCenterRepository : ISolutionCenterRepository
    {
        private readonly SitraCalcoContext _context;

        public SolutionCenterRepository(
            SitraCalcoContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SolutionCenterType>>
            GetSolutionCenterTypes()
        {
            return await _context.SolutionCenterTypes
                .AsNoTracking()
                .OrderBy(x => x.solution_center_type_id)
                .ToListAsync();
        }

        public async Task<bool> SolutionCenterTypeExists(
    long solutionCenterTypeId)
        {
            return await _context.SolutionCenterTypes
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_type_id ==
                    solutionCenterTypeId);
        }

        public async Task<bool> SolutionCenterCodeExists(string solutionCenterCode)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_code ==
                    solutionCenterCode);
        }

        public async Task<long> CreateSolutionCenter(SolutionCenter solutionCenter)
        {
            await _context.SolutionCenters
                .AddAsync(solutionCenter);

            await _context.SaveChangesAsync();

            return solutionCenter.solution_center_id;
        }

        public async Task<bool> SolutionCenterExists(long solutionCenterId)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_id == solutionCenterId);
        }

        public async Task<long> CreateSectionConfiguration(long solutionCenterId,Section section,IEnumerable<SectionProductDto> products,string createdBy)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                
                await _context.Sections.AddAsync(section);

                await _context.SaveChangesAsync();

                
                var sectionId = section.section_id;

                
                var solutionCenterProducts = products
                    .Select(product => new SolutionCenterProduct
                    {
                        solution_center_id = solutionCenterId,

                        section_id = sectionId,

                        product_id = product.ProductId,

                        sort_order = product.SortOrder,

                        created_by = createdBy.Trim(),

                        created_at = DateTime.Now
                    })
                    .ToList();

                await _context.SolutionCenterProducts
                    .AddRangeAsync(solutionCenterProducts);

                await _context.SaveChangesAsync();

                
                await transaction.CommitAsync();

                return sectionId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

        }

        public async Task<bool> SolutionCenterNameExists(string solutionCenterName)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_name == solutionCenterName);
        }

        public async Task<bool> SectionNameExists(long solutionCenterId,string sectionName)
        {
            return await (
                from solutionCenterProduct
                    in _context.SolutionCenterProducts.AsNoTracking()

                join section
                    in _context.Sections.AsNoTracking()
                    on solutionCenterProduct.section_id
                    equals section.section_id

                where
                    solutionCenterProduct.solution_center_id
                        == solutionCenterId
                    &&
                    section.section_name == sectionName

                select section.section_id
            )
            .AnyAsync();
        }

        public async Task<PagedDto<SolutionCenterListDto>>
    GetPagedSolutionCenters(
        int page,
        int take,
        long? solutionCenterTypeId = null)
        {
            if (page < 1)
                page = 1;

            if (take < 1)
                take = 10;

            var query =
                from solutionCenter in
                    _context.SolutionCenters.AsNoTracking()

                join solutionCenterType in
                    _context.SolutionCenterTypes.AsNoTracking()

                on solutionCenter.solution_center_type_id
                    equals solutionCenterType.solution_center_type_id

                select new
                {
                    SolutionCenter = solutionCenter,
                    SolutionCenterType = solutionCenterType
                };

            if (solutionCenterTypeId.HasValue)
            {
                query = query.Where(x =>
                    x.SolutionCenter.solution_center_type_id
                        == solutionCenterTypeId.Value);
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(x =>
                    x.SolutionCenter.solution_center_name)
                .Skip((page - 1) * take)
                .Take(take)
                .Select(x => new SolutionCenterListDto
                {
                    SolutionCenterId =
                        x.SolutionCenter.solution_center_id,

                    SolutionCenterTypeId =
                        x.SolutionCenter.solution_center_type_id,

                    SolutionCenterTypeName =
                        x.SolutionCenterType
                            .solution_center_type_name,

                    SolutionCenterCode =
                        x.SolutionCenter.solution_center_code,

                    SolutionCenterName =
                        x.SolutionCenter.solution_center_name,

                    IsActive =
                        x.SolutionCenter.is_active
                })
                .ToListAsync();

            var pages = total == 0
                ? 0
                : (int)Math.Ceiling(
                    total / (double)take);

            return new PagedDto<SolutionCenterListDto>
            {
                Items = items,
                Total = total,
                Page = page,
                Take = take,
                Pages = pages
            };
        }

        public async Task<SolutionCenterDetailDto?> GetSolutionCenterById(
    long solutionCenterId)
        {
            var solutionCenter = await (
                from center in
                    _context.SolutionCenters.AsNoTracking()

                join type in
                    _context.SolutionCenterTypes.AsNoTracking()

                on center.solution_center_type_id
                    equals type.solution_center_type_id

                where center.solution_center_id
                    == solutionCenterId

                select new SolutionCenterDetailDto
                {
                    SolutionCenterId =
                        center.solution_center_id,

                    SolutionCenterTypeId =
                        center.solution_center_type_id,

                    SolutionCenterTypeName =
                        type.solution_center_type_name,

                    SolutionCenterCode =
                        center.solution_center_code,

                    SolutionCenterName =
                        center.solution_center_name,

                    IsActive =
                        center.is_active
                }
            ).FirstOrDefaultAsync();

            if (solutionCenter is null)
                return null;

            
            var details = await (
                from association in
                    _context.SolutionCenterProducts.AsNoTracking()

                join section in
                    _context.Sections.AsNoTracking()

                on association.section_id
                    equals section.section_id

                join product in
                    _context.Products.AsNoTracking()

                on association.product_id
                    equals product.product_id

                where association.solution_center_id
                    == solutionCenterId

                orderby
                    section.section_id,
                    association.sort_order

                select new
                {
                    SectionId =
                        section.section_id,

                    SectionName =
                        section.section_name,

                    SectionIsActive =
                        section.is_active,

                    SolutionCenterProductId =
                        association.solution_center_product_id,

                    ProductId =
                        product.product_id,

                    ProductName =
                        product.product_name,

                    Reference =
                        product.reference,

                    UnitOfMeasure =
                        product.unit_of_measure,

                    PlanId =
                        product.plan_id,

                    SortOrder =
                        association.sort_order,

                    CreatedBy =
                        association.created_by,

                    CreatedAt =
                        association.created_at
                }
            ).ToListAsync();

            // 3. Agrupar los productos por Section.
            solutionCenter.Sections = details
                .GroupBy(x => new
                {
                    x.SectionId,
                    x.SectionName,
                    x.SectionIsActive
                })
                .Select(group =>
                    new SolutionCenterSectionDetailDto
                    {
                        SectionId =
                            group.Key.SectionId,

                        SectionName =
                            group.Key.SectionName,

                        IsActive =
                            group.Key.SectionIsActive,

                        Products = group
                            .OrderBy(x =>
                                x.SortOrder)
                            .Select(x =>
                                new SolutionCenterProductDetailDto
                                {
                                    SolutionCenterProductId =
                                        x.SolutionCenterProductId,

                                    ProductId =
                                        x.ProductId,

                                    ProductName =
                                        x.ProductName,

                                    Reference =
                                        x.Reference,

                                    UnitOfMeasure =
                                        x.UnitOfMeasure,

                                    PlanId =
                                        x.PlanId,

                                    SortOrder =
                                        x.SortOrder,

                                    CreatedBy =
                                        x.CreatedBy,

                                    CreatedAt =
                                        x.CreatedAt
                                })
                            .ToList()
                    })
                .ToList();

            return solutionCenter;
        }

        public async Task<bool> UpdateSolutionCenterStatus(
    long solutionCenterId,
    bool isActive)
        {
            var solutionCenter = await _context.SolutionCenters
                .FirstOrDefaultAsync(x =>
                    x.solution_center_id == solutionCenterId);

            if (solutionCenter is null)
                return false;

            solutionCenter.is_active = isActive;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateSectionStatus(
    long sectionId,
    bool isActive)
        {
            var section = await _context.Sections
                .FirstOrDefaultAsync(x =>
                    x.section_id == sectionId);

            if (section is null)
                return false;

            section.is_active = isActive;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SectionBelongsToSolutionCenter(
    long solutionCenterId,
    long sectionId)
        {
            return await _context.SolutionCenterProducts
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_id == solutionCenterId &&
                    x.section_id == sectionId);
        }

        public async Task<IEnumerable<Product>> GetSectionProducts(
    long solutionCenterId,
    long sectionId)
        {
            return await (
                from association in
                    _context.SolutionCenterProducts.AsNoTracking()

                join product in
                    _context.Products.AsNoTracking()

                on association.product_id
                    equals product.product_id

                where
                    association.solution_center_id == solutionCenterId
                    &&
                    association.section_id == sectionId

                select product
            ).ToListAsync();
        }

        public async Task<long> AddProductToSection(
    long solutionCenterId,
    long sectionId,
    long productId,
    int position,
    string createdBy)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                
                var productsToMove =
                    await _context.SolutionCenterProducts
                        .Where(x =>
                            x.solution_center_id == solutionCenterId &&
                            x.section_id == sectionId &&
                            x.sort_order >= position)
                        .OrderByDescending(x => x.sort_order)
                        .ToListAsync();

                
                foreach (var product in productsToMove)
                {
                    product.sort_order += 1;
                }

                var newAssociation =
                    new SolutionCenterProduct
                    {
                        solution_center_id =
                            solutionCenterId,

                        section_id =
                            sectionId,

                        product_id =
                            productId,

                        sort_order =
                            position,

                        created_by =
                            createdBy.Trim(),

                        created_at =
                            DateTime.Now
                    };

                await _context.SolutionCenterProducts
                    .AddAsync(newAssociation);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return newAssociation
                    .solution_center_product_id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> UpdateProductOrder(
    long solutionCenterId,
    long sectionId,
    long solutionCenterProductId,
    int newPosition)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Producto que queremos mover.
                var currentProduct =
                    await _context.SolutionCenterProducts
                        .FirstOrDefaultAsync(x =>
                            x.solution_center_product_id ==
                                solutionCenterProductId
                            &&
                            x.solution_center_id ==
                                solutionCenterId
                            &&
                            x.section_id ==
                                sectionId);

                if (currentProduct is null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                var currentPosition =
                    currentProduct.sort_order;

                // Si ya está en esa posición,
                // no hay nada que modificar.
                if (currentPosition == newPosition)
                {
                    await transaction.CommitAsync();
                    return true;
                }

                // Buscar el producto que actualmente
                // ocupa la nueva posición.
                var targetProduct =
                    await _context.SolutionCenterProducts
                        .FirstOrDefaultAsync(x =>
                            x.solution_center_id ==
                                solutionCenterId
                            &&
                            x.section_id ==
                                sectionId
                            &&
                            x.sort_order ==
                                newPosition);

                // La posición solicitada debe existir.
                if (targetProduct is null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // Intercambiar posiciones.
                currentProduct.sort_order =
                    newPosition;

                targetProduct.sort_order =
                    currentPosition;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> DeleteProductFromSection(
    long solutionCenterId,
    long sectionId,
    long solutionCenterProductId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Buscar exactamente la asociación que se quiere eliminar.
                var association =
                    await _context.SolutionCenterProducts
                        .FirstOrDefaultAsync(x =>
                            x.solution_center_product_id ==
                                solutionCenterProductId
                            &&
                            x.solution_center_id ==
                                solutionCenterId
                            &&
                            x.section_id ==
                                sectionId);

                if (association is null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // No permitir eliminar el último producto de la sección.
                var productsCount =
                    await _context.SolutionCenterProducts
                        .CountAsync(x =>
                            x.solution_center_id ==
                                solutionCenterId
                            &&
                            x.section_id ==
                                sectionId);

                if (productsCount <= 1)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                var deletedPosition =
                    association.sort_order;

                // Eliminar la asociación.
                _context.SolutionCenterProducts
                    .Remove(association);

                // Buscar todos los productos posteriores.
                var productsToMove =
                    await _context.SolutionCenterProducts
                        .Where(x =>
                            x.solution_center_id ==
                                solutionCenterId
                            &&
                            x.section_id ==
                                sectionId
                            &&
                            x.sort_order >
                                deletedPosition)
                        .OrderBy(x => x.sort_order)
                        .ToListAsync();

                // Correrlos una posición hacia atrás.
                foreach (var product in productsToMove)
                {
                    product.sort_order -= 1;
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}