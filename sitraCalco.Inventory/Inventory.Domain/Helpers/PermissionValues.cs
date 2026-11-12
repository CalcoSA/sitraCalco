namespace Inventory.Domain.Helpers
{
    public static class PermissionValues
    {
        public static string NormalizeKey(string? key)
        {
            return key?.Trim().ToUpperInvariant() ?? string.Empty;
        }

        public static string NormalizeRoles(string? value)
        {
            return string.Join(",", SplitRoles(value)
                .Select(role => role.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        public static bool ContainsRole(string? value, string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return SplitRoles(value).Any(configuredRole =>
                string.Equals(configuredRole, role.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static string[] SplitRoles(string? value)
        {
            return (value ?? string.Empty).Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
