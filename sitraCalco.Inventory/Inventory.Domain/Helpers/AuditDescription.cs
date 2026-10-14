using System.Globalization;

namespace Inventory.Domain.Helpers
{
    public static class AuditDescription
    {
        public static string Name(string? name, long id) =>
            string.IsNullOrWhiteSpace(name) ? $"sin nombre disponible (ID {id})" : name;

        // InventoryLog admite 500 caracteres. Acortar los nombres más largos
        // conserva el contexto de todas las entidades, cantidades y posiciones.
        public static string Format(FormattableString description)
        {
            var arguments = description.GetArguments();
            var text = string.Format(CultureInfo.InvariantCulture, description.Format, arguments);
            while (text.Length > 500)
            {
                var index = Enumerable.Range(0, arguments.Length)
                    .Where(i => arguments[i] is string value && value.Length > 4)
                    .OrderByDescending(i => ((string)arguments[i]!).Length)
                    .First();
                var value = (string)arguments[index]!;
                var length = value.Length - 1;
                arguments[index] = value[..(length - 3)] + "...";
                text = string.Format(CultureInfo.InvariantCulture, description.Format, arguments);
            }
            return text;
        }
    }
}
