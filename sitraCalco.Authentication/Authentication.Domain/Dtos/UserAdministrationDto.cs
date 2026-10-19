using System.Text.Json.Serialization;

namespace Authentication.Domain.Dtos
{
    public class UserAdministrationDto : UserDto
    {
        // Preserve the existing JSON for users without a PDV assignment.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SolutionCenterId { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SolutionCenterCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SolutionCenterName { get; set; }
    }
}
