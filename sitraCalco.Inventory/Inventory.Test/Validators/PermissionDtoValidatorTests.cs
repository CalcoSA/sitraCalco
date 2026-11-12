using Inventory.Domain.Dtos;
using Inventory.Domain.Validators;

namespace Inventory.Test.Validators
{
    public class PermissionDtoValidatorTests
    {
        public static IEnumerable<object?[]> InvalidValues()
        {
            yield return new object?[] { null, "COSTOS" };
            yield return new object?[] { "", "COSTOS" };
            yield return new object?[] { "   ", "COSTOS" };
            yield return new object?[] { new string('K', 151), "COSTOS" };
            yield return new object?[] { "VIEW_EXAMPLE", null };
            yield return new object?[] { "VIEW_EXAMPLE", "" };
            yield return new object?[] { "VIEW_EXAMPLE", "   " };
            yield return new object?[] { "VIEW_EXAMPLE", " , , " };
            yield return new object?[] { "VIEW_EXAMPLE", new string('R', 1001) };
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void CreateAndUpdate_ShouldApplySameValidation(string? key, string? value)
        {
            var create = new CreatePermissionDtoValidator().Validate(new CreatePermissionDto
            {
                PermissionKey = key, PermissionValue = value
            });
            var update = new UpdatePermissionDtoValidator().Validate(new UpdatePermissionDto
            {
                PermissionKey = key, PermissionValue = value
            });

            Assert.False(create.IsValid);
            Assert.False(update.IsValid);
            Assert.Equal(create.Errors.Select(x => x.ErrorMessage), update.Errors.Select(x => x.ErrorMessage));
        }

        [Theory]
        [InlineData("VIEW_EXAMPLE", " costos, ALMACEN ,costos ")]
        [InlineData("view_example", "Administrador,CONTROL INTERNO")]
        public void CreateAndUpdate_ShouldAcceptNormalizableValues(string key, string value)
        {
            Assert.True(new CreatePermissionDtoValidator().Validate(new CreatePermissionDto
            {
                PermissionKey = key, PermissionValue = value
            }).IsValid);
            Assert.True(new UpdatePermissionDtoValidator().Validate(new UpdatePermissionDto
            {
                PermissionKey = key, PermissionValue = value
            }).IsValid);
        }

        [Fact]
        public void CreateAndUpdate_ShouldAcceptMaximumLengths()
        {
            Assert.True(new CreatePermissionDtoValidator().Validate(new CreatePermissionDto
            {
                PermissionKey = new string('K', 150), PermissionValue = new string('R', 1000)
            }).IsValid);
            Assert.True(new UpdatePermissionDtoValidator().Validate(new UpdatePermissionDto
            {
                PermissionKey = new string('K', 150), PermissionValue = new string('R', 1000)
            }).IsValid);
        }
    }
}
