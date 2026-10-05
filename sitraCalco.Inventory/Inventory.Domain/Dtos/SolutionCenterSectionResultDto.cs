namespace Inventory.Domain.Dtos
{
    public enum SolutionCenterSectionStatus
    {
        InvalidRequest,
        SolutionCenterNotFound,
        SectionNotFound,
        SectionInactive,
        AssignmentNotFound,
        AlreadyAssigned,
        Success
    }

    // Resultado interno para validaciones y auditoría; no se expone como contrato HTTP.
    public class SolutionCenterSectionResultDto
    {
        public SolutionCenterSectionStatus Status { get; set; }

        public string SolutionCenterName { get; set; } = string.Empty;

        public string SectionName { get; set; } = string.Empty;

        public bool WasReactivated { get; set; }
    }
}
