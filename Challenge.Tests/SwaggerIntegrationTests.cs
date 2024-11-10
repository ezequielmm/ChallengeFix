using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net;
using Challenge; // Asegúrate de que este namespace corresponde a tu proyecto principal
using Microsoft.AspNetCore.Hosting;

namespace Challenge.Tests.Integration
{
    public class SwaggerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SwaggerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Verifica que Swagger está habilitado en entornos Development y Staging.
        /// </summary>
        /// <param name="environment">El entorno a probar.</param>
        [Theory]
        [InlineData("Development")]
        [InlineData("Staging")]
        public async Task Swagger_IsEnabled_InDevelopmentAndStaging(string environment)
        {
            // Arrange
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
            });

            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.MediaType.Should().Be("text/html");
        }

        /// <summary>
        /// Verifica que Swagger está deshabilitado en otros entornos (e.g., Production).
        /// </summary>
        /// <param name="environment">El entorno a probar.</param>
        [Theory]
        [InlineData("Production")]
        [InlineData("Testing")] // Puedes agregar otros entornos según sea necesario
        public async Task Swagger_IsDisabled_InOtherEnvironments(string environment)
        {
            // Arrange
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
            });

            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
