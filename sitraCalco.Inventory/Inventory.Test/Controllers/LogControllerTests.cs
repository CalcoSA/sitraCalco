using Inventory.Api.Controllers;
using Inventory.Application.Interfaces;
using Inventory.Domain.Dtos;
using Inventory.Domain.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.Test.Controllers
{
    public class LogControllerTests
    {
        private readonly Mock<ILogApplication>
            _logApplicationMock;

        private readonly Mock<ILogger<LogController>>
            _loggerMock;

        private readonly LogController
            _controller;

        public LogControllerTests()
        {
            _logApplicationMock =
                new Mock<ILogApplication>();

            _loggerMock =
                new Mock<ILogger<LogController>>();

            _controller =
                new LogController(
                    _logApplicationMock.Object,
                    _loggerMock.Object);
        }

        // =========================================================
        // VALIDACIONES DE PAGINACIÓN
        // =========================================================

        [Theory]
        [InlineData(0, 10)]
        [InlineData(-1, 10)]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        [InlineData(0, 0)]
        public async Task GetPaged_ShouldReturnBadRequest_WhenPageOrTakeIsInvalid(
            int page,
            int take)
        {
            // Act
            var result =
                await _controller.GetPaged(
                    null,
                    null,
                    page,
                    take);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "Page y Take deben ser mayores a cero.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.GetPaged(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        // =========================================================
        // VALIDACIÓN DEL RANGO DE FECHAS
        // =========================================================

        [Fact]
        public async Task GetPaged_ShouldReturnBadRequest_WhenFromIsAfterTo()
        {
            // Arrange
            var from =
                new DateTime(2026, 9, 20);

            var to =
                new DateTime(2026, 9, 10);

            // Act
            var result =
                await _controller.GetPaged(
                    from,
                    to,
                    1,
                    10);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "La fecha desde no puede ser mayor que la fecha hasta.",
                response.Message);

            _logApplicationMock.Verify(
                x => x.GetPaged(
                    It.IsAny<DateTime?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPaged_ShouldAllowSameDate_WhenTimesAreDifferent()
        {
            // Arrange
            var from =
                new DateTime(
                    2026,
                    9,
                    18,
                    20,
                    0,
                    0);

            var to =
                new DateTime(
                    2026,
                    9,
                    18,
                    8,
                    0,
                    0);

            var logs =
                CreatePagedLogsWithItems();

            _logApplicationMock
                .Setup(x => x.GetPaged(
                    from,
                    to,
                    1,
                    10))
                .ReturnsAsync(logs);

            // Act
            var result =
                await _controller.GetPaged(
                    from,
                    to,
                    1,
                    10);

            // Assert
            Assert.IsType<OkObjectResult>(
                result);

            _logApplicationMock.Verify(
                x => x.GetPaged(
                    from,
                    to,
                    1,
                    10),
                Times.Once);
        }

        // =========================================================
        // APPLICATION RETORNA NULL
        // =========================================================

        [Fact]
        public async Task GetPaged_ShouldReturnBadRequest_WhenApplicationReturnsNull()
        {
            // Arrange
            DateTime? from =
                new DateTime(2026, 9, 1);

            DateTime? to =
                new DateTime(2026, 9, 30);

            _logApplicationMock
                .Setup(x => x.GetPaged(
                    from,
                    to,
                    1,
                    10))
                .ReturnsAsync(
                    (PagedDto<LogDto>?)null);

            // Act
            var result =
                await _controller.GetPaged(
                    from,
                    to,
                    1,
                    10);

            // Assert
            var badRequest =
                Assert.IsType<BadRequestObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    badRequest.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "El rango de fechas no es válido.",
                response.Message);
        }

        // =========================================================
        // SIN LOGS
        // =========================================================

        [Fact]
        public async Task GetPaged_ShouldReturnOkWithIsSuccessFalse_WhenNoLogsExist()
        {
            // Arrange
            var logs =
                new PagedDto<LogDto>
                {
                    Items =
                        new List<LogDto>(),

                    Total = 0,

                    Page = 1,

                    Take = 10,

                    Pages = 0
                };

            _logApplicationMock
                .Setup(x => x.GetPaged(
                    null,
                    null,
                    1,
                    10))
                .ReturnsAsync(logs);

            // Act
            var result =
                await _controller.GetPaged(
                    null,
                    null,
                    1,
                    10);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "No se encontraron registros de actividad.",
                response.Message);

            Assert.Same(
                logs,
                response.Result);
        }

        // =========================================================
        // CONSULTA CORRECTA
        // =========================================================

        [Fact]
        public async Task GetPaged_ShouldReturnOk_WhenLogsExist()
        {
            // Arrange
            var logs =
                CreatePagedLogsWithItems();

            _logApplicationMock
                .Setup(x => x.GetPaged(
                    null,
                    null,
                    1,
                    10))
                .ReturnsAsync(logs);

            // Act
            var result =
                await _controller.GetPaged(
                    null,
                    null,
                    1,
                    10);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            Assert.Equal(
                "Logs consultados correctamente.",
                response.Message);

            Assert.Same(
                logs,
                response.Result);
        }

        [Fact]
        public async Task GetPaged_ShouldSendCorrectParametersToApplication()
        {
            // Arrange
            DateTime? from =
                new DateTime(
                    2026,
                    9,
                    1);

            DateTime? to =
                new DateTime(
                    2026,
                    9,
                    18);

            const int page = 2;
            const int take = 25;

            var logs =
                CreatePagedLogsWithItems(
                    page,
                    take);

            _logApplicationMock
                .Setup(x => x.GetPaged(
                    from,
                    to,
                    page,
                    take))
                .ReturnsAsync(logs);

            // Act
            await _controller.GetPaged(
                from,
                to,
                page,
                take);

            // Assert
            _logApplicationMock.Verify(
                x => x.GetPaged(
                    from,
                    to,
                    page,
                    take),
                Times.Once);
        }

        [Fact]
        public async Task GetPaged_ShouldWorkWithoutDateFilters()
        {
            // Arrange
            var logs =
                CreatePagedLogsWithItems();

            _logApplicationMock
                .Setup(x => x.GetPaged(
                    null,
                    null,
                    1,
                    10))
                .ReturnsAsync(logs);

            // Act
            var result =
                await _controller.GetPaged();

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            _logApplicationMock.Verify(
                x => x.GetPaged(
                    null,
                    null,
                    1,
                    10),
                Times.Once);
        }

        [Fact]
        public async Task GetPaged_ShouldReturnCorrectPagedResult()
        {
            // Arrange
            var logs =
                new PagedDto<LogDto>
                {
                    Items =
                        new List<LogDto>
                        {
                            new LogDto(),
                            new LogDto()
                        },

                    Total = 12,

                    Page = 2,

                    Take = 5,

                    Pages = 3
                };

            _logApplicationMock
                .Setup(x => x.GetPaged(
                    null,
                    null,
                    2,
                    5))
                .ReturnsAsync(logs);

            // Act
            var result =
                await _controller.GetPaged(
                    null,
                    null,
                    2,
                    5);

            // Assert
            var ok =
                Assert.IsType<OkObjectResult>(
                    result);

            var response =
                Assert.IsType<ResponseApi>(
                    ok.Value);

            Assert.True(
                response.IsSuccess);

            var responseLogs =
                Assert.IsType<PagedDto<LogDto>>(
                    response.Result);

            Assert.Equal(
                2,
                responseLogs.Items.Count());

            Assert.Equal(
                12,
                responseLogs.Total);

            Assert.Equal(
                2,
                responseLogs.Page);

            Assert.Equal(
                5,
                responseLogs.Take);

            Assert.Equal(
                3,
                responseLogs.Pages);
        }

        // =========================================================
        // EXCEPCIONES
        // =========================================================

        [Fact]
        public async Task GetPaged_ShouldReturn500_WhenApplicationThrowsException()
        {
            // Arrange
            _logApplicationMock
                .Setup(x => x.GetPaged(
                    null,
                    null,
                    1,
                    10))
                .ThrowsAsync(
                    new Exception(
                        "Error de prueba"));

            // Act
            var result =
                await _controller.GetPaged(
                    null,
                    null,
                    1,
                    10);

            // Assert
            var objectResult =
                Assert.IsType<ObjectResult>(
                    result);

            Assert.Equal(
                500,
                objectResult.StatusCode);

            var response =
                Assert.IsType<ResponseApi>(
                    objectResult.Value);

            Assert.False(
                response.IsSuccess);

            Assert.Equal(
                "Ocurrió un error al consultar los logs.",
                response.Message);
        }

        // =========================================================
        // MÉTODOS AUXILIARES
        // =========================================================

        private static PagedDto<LogDto>
            CreatePagedLogsWithItems(
                int page = 1,
                int take = 10)
        {
            return new PagedDto<LogDto>
            {
                Items =
                    new List<LogDto>
                    {
                        new LogDto()
                    },

                Total = 1,

                Page = page,

                Take = take,

                Pages = 1
            };
        }
    }
}