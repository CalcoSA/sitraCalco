using KitchenTraceability.Infrastructure.Persistance.Data;
using KitchenTraceability.Domain.Interfaces;
using KitchenTraceability.Domain.Models;

namespace KitchenTraceability.Infrastructure.Persistance.Repositories
{
    public class ReceptionTransportEvaluationRepository : Repository<ReceptionTransportEvaluation>, IReceptionTransportEvaluationRepository
    {
        private readonly SitraCalcoContext _context;

        public ReceptionTransportEvaluationRepository(SitraCalcoContext context) : base(context)
        {
            _context = context;
        }
    }
}