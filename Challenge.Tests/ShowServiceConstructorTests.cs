using Xunit;
using Moq;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Challenge.Services; // Reemplaza con tu espacio de nombres real
using Challenge.Repositories; // Reemplaza con tu espacio de nombres real
using Challenge.Data; // Reemplaza con tu espacio de nombres real

namespace Challenge.Tests.Services
{
    public class ShowServiceConstructorTests
    {
        private Mock<IShowRepository> _showRepositoryMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private IConfiguration _configuration;
        private ApplicationDbContext _context;

        public ShowServiceConstructorTests()
        {
            // Inicializar los mocks
            _showRepositoryMock = new Mock<IShowRepository>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Configuración de la base de datos en memoria
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ShowServiceConstructorTestDb")
                .Options;
            _context = new ApplicationDbContext(options);
        }

        // Helper method para crear IConfiguration con ApiUrl
        private IConfiguration GetConfiguration(string apiUrl = "http://api.tvmaze.com/")
        {
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string> {
                {"ApiUrl", apiUrl}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        /// <summary>
        /// Test que verifica que el constructor de ShowService crea una instancia correctamente cuando todas las dependencias son válidas.
        /// </summary>
        [Fact]
        public void Constructor_ShouldCreateInstance_WhenAllDependenciesAreProvided()
        {
            // Arrange
            _configuration = GetConfiguration("http://api.tvmaze.com/");

            // Act
            var service = new ShowService(
                _showRepositoryMock.Object,
                _context,
                _httpClientFactoryMock.Object,
                _configuration
            );

            // Assert
            Assert.NotNull(service);
        }

        /// <summary>
        /// Test que verifica que el constructor lanza ArgumentNullException cuando showRepository es null.
        /// </summary>
        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenShowRepositoryIsNull()
        {
            // Arrange
            _configuration = GetConfiguration("http://api.tvmaze.com/");

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new ShowService(
                null, // showRepository es null
                _context,
                _httpClientFactoryMock.Object,
                _configuration
            ));

            Assert.Equal("showRepository", exception.ParamName);
        }

        /// <summary>
        /// Test que verifica que el constructor lanza ArgumentNullException cuando context es null.
        /// </summary>
        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
        {
            // Arrange
            _configuration = GetConfiguration("http://api.tvmaze.com/");

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new ShowService(
                _showRepositoryMock.Object,
                null, // context es null
                _httpClientFactoryMock.Object,
                _configuration
            ));

            Assert.Equal("context", exception.ParamName);
        }

        /// <summary>
        /// Test que verifica que el constructor lanza ArgumentNullException cuando httpClientFactory es null.
        /// </summary>
        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenHttpClientFactoryIsNull()
        {
            // Arrange
            _configuration = GetConfiguration("http://api.tvmaze.com/");

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new ShowService(
                _showRepositoryMock.Object,
                _context,
                null, // httpClientFactory es null
                _configuration
            ));

            Assert.Equal("httpClientFactory", exception.ParamName);
        }

        /// <summary>
        /// Test que verifica que el constructor lanza ArgumentNullException cuando configuration es null.
        /// </summary>
        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenConfigurationIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new ShowService(
                _showRepositoryMock.Object,
                _context,
                _httpClientFactoryMock.Object,
                null // configuration es null
            ));

            Assert.Equal("configuration", exception.ParamName);
        }

        /// <summary>
        /// Test que verifica que el constructor lanza ArgumentException cuando ApiUrl está vacío o contiene solo espacios en blanco.
        /// </summary>
        /// <param name="apiUrl">El valor de ApiUrl a probar.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_ShouldThrowArgumentException_WhenApiUrlIsEmptyOrWhitespace(string apiUrl)
        {
            // Arrange
            _configuration = GetConfiguration(apiUrl);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ShowService(
                _showRepositoryMock.Object,
                _context,
                _httpClientFactoryMock.Object,
                _configuration
            ));

            Assert.Equal("configuration", exception.ParamName);
            Assert.Contains("ApiUrl configuration is missing or empty.", exception.Message);
        }
    }
}
