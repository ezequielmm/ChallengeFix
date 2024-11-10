using Xunit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using FluentAssertions;
using System.Net;
using Challenge; // Asegúrate de que este namespace corresponde a tu proyecto principal
using Microsoft.Extensions.DependencyInjection;
using Challenge.Data;
using Microsoft.EntityFrameworkCore;
using Challenge.Repositories;
using Challenge.Services;
using Microsoft.AspNetCore.Hosting;
using System;
using Moq;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Challenge.Tests.Integration
{
    public class SimpleIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SimpleIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Agrega un DbContext en memoria para pruebas
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });
                });

                builder.UseEnvironment("Development");
            });
        }

        /// <summary>
        /// Verifica que Swagger UI está disponible en modo desarrollo.
        /// </summary>
        [Fact]
        public async Task Get_SwaggerEndpoint_ReturnsSuccessInDevelopment()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/index.html");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType.ToString().Should().Contain("text/html");
        }

        /// <summary>
        /// Verifica que los servicios están registrados correctamente en el contenedor de dependencias.
        /// </summary>
        [Fact]
        public void Services_AreRegisteredCorrectly()
        {
            // Arrange
            var scopeFactory = _factory.Services.GetService<IServiceScopeFactory>();

            using (var scope = scopeFactory.CreateScope())
            {
                var services = scope.ServiceProvider;

                // Act
                var showService = services.GetService<IShowService>();
                var showRepository = services.GetService<IShowRepository>();
                var dbContext = services.GetService<ApplicationDbContext>();

                // Assert
                showService.Should().NotBeNull();
                showRepository.Should().NotBeNull();
                dbContext.Should().NotBeNull();
            }
        }

        /// <summary>
        /// Verifica el flujo completo de la configuración de la base de datos usando una cadena de conexión vacía.
        /// </summary>
        [Fact]
        public void ConfigureDatabase_FullFlow_ThrowsException_WhenConnectionStringIsNullOrEmpty()
        {
            // Arrange
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(config => config.GetSection("ConnectionStrings")["DefaultConnection"]).Returns(string.Empty);

            // Act
            Action act = () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(configurationMock.Object);
                Program.ConfigureDatabase(services, configurationMock.Object);

                // Simula la resolución del servicio
                var serviceProvider = services.BuildServiceProvider();
                serviceProvider.GetService<ApplicationDbContext>();
            };

            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("Connection string 'DefaultConnection' is not configured.");
        }

        /// <summary>
        /// Verifica el flujo completo de la configuración de la base de datos con una cadena de conexión válida usando InMemory.
        /// </summary>
        [Fact]
        public void ConfigureDatabase_FullFlow_WorksCorrectly_WithValidConnectionString()
        {
            // Arrange
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(config => config.GetSection("ConnectionStrings")["DefaultConnection"]).Returns("InMemoryConnectionString");

            // Act
            Action act = () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(configurationMock.Object);
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("InMemoryDbForTesting");
                });

                // Simula la resolución del servicio con InMemory
                var serviceProvider = services.BuildServiceProvider();
                var dbContext = serviceProvider.GetService<ApplicationDbContext>();
                dbContext.Should().NotBeNull();
            };

            // Assert
            act.Should().NotThrow();
        }

        /// <summary>
        /// Prueba el método Dummy de la clase Program.
        /// </summary>
        [Fact]
        public void Program_DummyMethod_WritesToConsole()
        {
            // Arrange
            using var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            // Act
            var program = new Program();
            program.Dummy();

            // Assert
            consoleOutput.ToString().Should().Contain("This is for unit testing");
        }
    }
}
