using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.Test.Helpers
{
    public static class ControllerIdentityExtensions
    {
        public static T WithIdentity<T>(
            this T controller,
            string? userLogin = "juan.zapata",
            string? role = "COSTOS") where T : ControllerBase
        {
            var claims = new List<Claim>
            {
                new Claim("idUser", "1"),
                new Claim("userName", "Juan Zapata")
            };

            if (userLogin is not null)
                claims.Add(new Claim("userLogin", userLogin));

            if (role is not null)
                claims.Add(new Claim("nameRole", role));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"))
                }
            };

            return controller;
        }
    }
}
