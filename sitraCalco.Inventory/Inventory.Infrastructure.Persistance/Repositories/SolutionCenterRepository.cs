using Inventory.Domain.Dtos;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Models;
using Inventory.Infrastructure.Persistance.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

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

        public async Task<long> CreateSectionConfiguration(long solutionCenterId,Section section,List<SectionProductDto> products, string createdBy)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                
                await _context.Sections.AddAsync(section);

                await _context.SaveChangesAsync();

                
                var sectionId = section.section_id;

                await _context.SolutionCenterSections.AddAsync(new SolutionCenterSection
                {
                    solution_center_id = solutionCenterId,
                    section_id = sectionId,
                    is_active = true
                });

                
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

            // Las secciones pertenecen al centro aunque todavía no tengan productos.
            var sections = await (
                from assignment in _context.SolutionCenterSections.AsNoTracking()
                join section in _context.Sections.AsNoTracking()
                    on assignment.section_id equals section.section_id
                where assignment.solution_center_id == solutionCenterId
                orderby section.section_id
                select new SolutionCenterSectionDetailDto
                {
                    SectionId = section.section_id,
                    SectionName = section.section_name,
                    IsActive = section.is_active,
                    AssignmentIsActive = assignment.is_active
                }
            ).ToListAsync();

            // Una consulta para todos los productos del centro, sin N+1 por sección.
            var products = await (
                from association in _context.SolutionCenterProducts.AsNoTracking()
                join assignment in _context.SolutionCenterSections.AsNoTracking()
                    on new { association.solution_center_id, association.section_id }
                    equals new { assignment.solution_center_id, assignment.section_id }
                join product in _context.Products.AsNoTracking()
                    on association.product_id equals product.product_id
                where association.solution_center_id == solutionCenterId
                orderby association.sort_order
                select new
                {
                    association.section_id,
                    Product = new SolutionCenterProductDetailDto
                    {
                        SolutionCenterProductId = association.solution_center_product_id,
                        ProductId = product.product_id,
                        ProductName = product.product_name,
                        Reference = product.reference,
                        UnitOfMeasure = product.unit_of_measure,
                        PlanId = product.plan_id,
                        SortOrder = association.sort_order,
                        CreatedBy = association.created_by,
                        CreatedAt = association.created_at
                    }
                }
            ).ToListAsync();

            var productsBySection = products.ToLookup(item => item.section_id);
            foreach (var section in sections)
            {
                section.Products = productsBySection[section.SectionId]
                    .Select(item => item.Product)
                    .ToList();
            }

            solutionCenter.Sections = sections;
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
            return await _context.SolutionCenterSections
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_id == solutionCenterId &&
                    x.section_id == sectionId &&
                    x.is_active);
        }

        public async Task<Section?> GetSectionById(long sectionId)
        {
            return await _context.Sections
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.section_id == sectionId);
        }

        public async Task<SolutionCenterSection?> GetSectionAssignment(
            long solutionCenterId,
            long sectionId)
        {
            return await _context.SolutionCenterSections
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.solution_center_id == solutionCenterId &&
                    x.section_id == sectionId);
        }

        public async Task<bool> CreateSectionAssignment(SolutionCenterSection assignment)
        {
            await _context.SolutionCenterSections.AddAsync(assignment);

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex) when
                (ex.InnerException is MySqlException { Number: 1062 })
            {
                // El UNIQUE centro/sección también protege ante asignaciones simultáneas.
                _context.Entry(assignment).State = EntityState.Detached;
                return false;
            }
        }

        public async Task<bool> UpdateSectionAssignmentStatus(
            long solutionCenterId,
            long sectionId,
            bool isActive)
        {
            return await _context.SolutionCenterSections
                .Where(x =>
                    x.solution_center_id == solutionCenterId &&
                    x.section_id == sectionId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.is_active, isActive)) > 0;
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
        public async Task<bool> SolutionCenterCodeExists(
    string solutionCenterCode,
    long excludeSolutionCenterId)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_code == solutionCenterCode
                    &&
                    x.solution_center_id != excludeSolutionCenterId);
        }
        public async Task<bool> SolutionCenterNameExists(
    string solutionCenterName,
    long excludeSolutionCenterId)
        {
            return await _context.SolutionCenters
                .AsNoTracking()
                .AnyAsync(x =>
                    x.solution_center_name == solutionCenterName
                    &&
                    x.solution_center_id != excludeSolutionCenterId);
        }
        public async Task<bool> UpdateSolutionCenter(
    long solutionCenterId,
    string solutionCenterCode,
    string solutionCenterName)
        {
            var solutionCenter =
                await _context.SolutionCenters
                    .FirstOrDefaultAsync(x =>
                        x.solution_center_id ==
                        solutionCenterId);

            if (solutionCenter is null)
                return false;

            solutionCenter.solution_center_code =
                solutionCenterCode;

            solutionCenter.solution_center_name =
                solutionCenterName;

            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<bool> SectionNameExists(
    string sectionName)
        {
            var normalizedName =
                sectionName.Trim().ToUpper();

            return await _context.Sections
                .AsNoTracking()
                .AnyAsync(section =>
                    section.section_name.ToUpper() ==
                    normalizedName);
        }
    }
}
