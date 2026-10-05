using System.Security.Claims;

namespace Inventory.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserLogin(this ClaimsPrincipal user)
        {
            return GetAuthenticatedClaim(user, "userLogin");
        }

        public static string? GetRoleName(this ClaimsPrincipal user)
        {
            return GetAuthenticatedClaim(user, "nameRole");
        }

        private static string? GetAuthenticatedClaim(ClaimsPrincipal user, string claimType)
        {
            // Solo se usan identidades autenticadas por el middleware de la API.
            var claims = user.Identities
                .Where(identity => identity.IsAuthenticated)
                .SelectMany(identity => identity.FindAll(claimType))
                .ToList();

            // Un claim ausente, vacío o ambiguo no puede identificar al actor o su rol.
            return claims.Count == 1 && !string.IsNullOrWhiteSpace(claims[0].Value)
                ? claims[0].Value.Trim()
                : null;
        }
    }
}
