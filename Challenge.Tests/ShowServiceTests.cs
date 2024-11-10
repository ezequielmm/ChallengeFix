// Usings necesarios
using Xunit;
using Moq;
using Moq.Protected;
using System.Net.Http;
using System.Threading.Tasks;
using Challenge.Services;
using Challenge.Repositories;
using Challenge.Data;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using Challenge.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Challenge.Models;
using System.Linq;
using System;
using Microsoft.Extensions.Configuration;
using Challenge.DTO;

namespace Challenge.Tests.Services
{
    public class ShowServiceTests : IDisposable
    {
        // Campos privados
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
        private ApplicationDbContext _context;
        private Mock<IShowRepository> _showRepositoryMock;
        private IShowRepository _showRepository; // Campo para la instancia real del repositorio
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private ShowService _showService;
        private readonly IConfiguration _configuration;

        public ShowServiceTests()
        {
            // Configuración de la base de datos en memoria con un nombre único para cada prueba
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Inicializar el mock de IHttpClientFactory
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Configuración in-memory para IConfiguration
            var inMemorySettings = new Dictionary<string, string> {
                {"ApiUrl", "http://api.tvmaze.com/"},
                {"ApiKey", "67890-klmno-12345-pqrst"},
                {"ConnectionStrings:DefaultConnection", "Server=(localdb)\\mssqllocaldb;Database=TvMazeShowsDb;Trusted_Connection=True;MultipleActiveResultSets=true"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        /// <summary>
        /// Inicializa el servicio con un repositorio real o simulado según el parámetro.
        /// </summary>
        /// <param name="useRealRepository">Determina si se usa un repositorio real.</param>
        private void InitializeService(bool useRealRepository = false)
        {
            // Inicializar el contexto
            _context = new ApplicationDbContext(_dbContextOptions);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            if (useRealRepository)
            {
                // Inicializar una instancia real del repositorio
                _showRepository = new ShowRepository(_context);
                _showService = new ShowService(_showRepository, _context, _httpClientFactoryMock.Object, _configuration);
            }
            else
            {
                // Inicializar el mock del repositorio
                _showRepositoryMock = new Mock<IShowRepository>();
                _showService = new ShowService(_showRepositoryMock.Object, _context, _httpClientFactoryMock.Object, _configuration);
            }
        }

        /// <summary>
        /// Crea un HttpClient simulado que devuelve una lista de shows en formato JSON.
        /// </summary>
        /// <param name="showsList">La lista de shows a devolver.</param>
        /// <param name="statusCode">El código de estado HTTP a devolver.</param>
        /// <returns>Un HttpClient configurado con el handler simulado.</returns>
        private HttpClient CreateMockHttpClient(List<ShowDto> showsList, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            string jsonResponse = showsList != null
                ? JsonSerializer.Serialize(showsList, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                : string.Empty;

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = statusCode,
                   Content = new StringContent(jsonResponse),
               })
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(_configuration["ApiUrl"]) // Usar ApiUrl de la configuración
            };

            return httpClient;
        }

        /// <summary>
        /// Implementación de IDisposable para limpiar recursos después de cada prueba.
        /// </summary>
        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Pruebas

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync agrega correctamente shows desde la API cuando la respuesta es válida.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldFetchAndStoreShows_WhenApiResponseIsValid()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 1,
                    Name = "Test Show",
                    Language = "English",
                    Genres = new List<string> { "Drama", "Action" },
                    Externals = new ExternalsDto { Imdb = "tt1234567", Tvrage = 123, Thetvdb = 456 },
                    Rating = new RatingDto { Average = 8.5 },
                    Network = new NetworkDto
                    {
                        Id = 10,
                        Name = "Test Network",
                        Country = new CountryDto
                        {
                            Code = "US",
                            Name = "United States",
                            Timezone = "America/New_York"
                        }
                    }
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(It.IsAny<int>())).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 1 && s.Name == "Test Show")), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync omite shows que ya existen en la base de datos.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldSkipExistingShows()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var existingShow = new Show { Id = 2, Name = "Existing Show" };
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(2)).ReturnsAsync(existingShow);

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 2,
                    Name = "Existing Show",
                    Genres = new List<string>(),
                    Externals = null,
                    Rating = null,
                    Network = null
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.IsAny<Show>()), Times.Never);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente shows con campos externos nulos.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_NullExternals()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 3,
                    Name = "Show Without Externals",
                    Language = "English",
                    Genres = new List<string> { "Comedy" },
                    Externals = null,
                    Rating = new RatingDto { Average = 7.0 },
                    Network = null
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(3)).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 3 && s.Name == "Show Without Externals")), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente shows con campos de rating nulos.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_NullRating()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 4,
                    Name = "Show Without Rating",
                    Language = "English",
                    Genres = new List<string> { "Sci-Fi" },
                    Externals = new ExternalsDto { Imdb = "tt7654321" },
                    Rating = null,
                    Network = null
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(4)).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 4 && s.Name == "Show Without Rating")), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente shows sin network asociado.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_NullNetwork()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 5,
                    Name = "Show Without Network",
                    Language = "English",
                    Genres = new List<string> { "Horror" },
                    Externals = new ExternalsDto { Imdb = "tt1122334" },
                    Rating = new RatingDto { Average = 6.5 },
                    Network = null
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(5)).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 5 && s.Name == "Show Without Network")), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente networks sin country asociado.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_NetworkWithNullCountry()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 6,
                    Name = "Show With Network Without Country",
                    Language = "English",
                    Genres = new List<string> { "Mystery" },
                    Externals = new ExternalsDto { Imdb = "tt9988776" },
                    Rating = new RatingDto { Average = 7.5 },
                    Network = new NetworkDto
                    {
                        Id = 20,
                        Name = "Network Without Country",
                        Country = null
                    }
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(6)).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 6 && s.Name == "Show With Network Without Country")), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente networks con country con código vacío.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_NetworkWithCountryHavingEmptyCode()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 7,
                    Name = "Show With Network With Country Without Code",
                    Language = "English",
                    Genres = new List<string> { "Fantasy" },
                    Externals = null,
                    Rating = null,
                    Network = new NetworkDto
                    {
                        Id = 30,
                        Name = "Network With Country Without Code",
                        Country = new CountryDto
                        {
                            Code = "",
                            Name = "Unknown Country",
                            Timezone = "UTC"
                        }
                    }
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(7)).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 7 && s.Name == "Show With Network With Country Without Code")), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente shows sin géneros asociados.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_ShowWithNoGenres()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 8,
                    Name = "Show Without Genres",
                    Language = "English",
                    Genres = null,
                    Externals = null,
                    Rating = null,
                    Network = null
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(8)).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 8 && s.Name == "Show Without Genres")), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync lanza una HttpRequestException cuando la respuesta de la API no es exitosa.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldProcessResponse_WhenApiResponseIsSuccessful()
        {
            // Arrange

            // Crear una lista de shows para simular una respuesta exitosa
            var showsList = new List<ShowDto>
            {
                new ShowDto { Id = 1, Name = "Show 1", Language = "English", Genres = new List<string> { "Drama" } },
                new ShowDto { Id = 2, Name = "Show 2", Language = "Spanish", Genres = new List<string> { "Comedy" } }
            };

            // Crear el mock del HttpMessageHandler
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            // Configurar SendAsync para devolver una respuesta exitosa (OK) cuando se realiza una solicitud GET a "http://api.tvmaze.com/shows"
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(req =>
                      req.Method == HttpMethod.Get &&
                      req.RequestUri == new Uri("http://api.tvmaze.com/shows")),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(JsonSerializer.Serialize(showsList)),
               })
               .Verifiable();

            // Crear el HttpClient simulado con el handler mockeado
            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(_configuration["ApiUrl"])
            };

            // Configurar el mock de IHttpClientFactory para devolver el HttpClient simulado
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Inicializar el servicio después de configurar los mocks
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert

            // Verificar que SendAsync fue llamado exactamente una vez con los parámetros correctos
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                   req.Method == HttpMethod.Get
                   && req.RequestUri == new Uri("http://api.tvmaze.com/shows")),
               ItExpr.IsAny<CancellationToken>()
            );

            // Verificar que los shows fueron agregados al repositorio y se guardaron los cambios
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.IsAny<Show>()), Times.Exactly(showsList.Count));
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once());
        }

        //[Fact]
        //public async Task AddShowAsync_ShouldNotCreateCountry_WhenCountryAlreadyExists()
        //{
        //    // Arrange
        //    string countryName = "Spain";

        //    var existingCountry = new Country
        //    {
        //        Id = 1,
        //        Name = countryName,
        //        Code = "ES",
        //        Timezone = "Europe/Madrid"
        //    };

        //    // Agregar el país existente al contexto
        //    _context.Countries.Add(existingCountry);
        //    await _context.SaveChangesAsync();

        //    var show = new Show
        //    {
        //        Id = 100,
        //        Name = "Test Show",
        //        Language = "English",
        //        Genres = new List<Genre> { new Genre { Name = "Drama" } },
        //        Country = existingCountry // Asociar el show al país existente
        //    };

        //    // Act
        //    await _showService.AddShowAsync(show);

        //    // Assert
        //    // Verificar que el show fue agregado correctamente
        //    var addedShow = await _context.Shows.Include(s => s.Country).Include(s => s.Genres).FirstOrDefaultAsync(s => s.Id == show.Id);
        //    Assert.NotNull(addedShow);
        //    Assert.Equal(show.Name, addedShow.Name);
        //    Assert.Equal(show.Language, addedShow.Language);
        //    Assert.Equal(existingCountry.Id, addedShow.Country.Id);
        //    Assert.Single(addedShow.Genres);
        //    Assert.Equal("Drama", addedShow.Genres.First().Name);

        //    // Verificar que no se haya creado un nuevo país
        //    var countries = await _context.Countries.ToListAsync();
        //    Assert.Single(countries); // Solo debe existir el país "Spain"
        //    Assert.Equal(countryName, countries.First().Name);

        //    // Opcional: Verificar que el país asociado al show es el existente
        //    Assert.Equal(existingCountry.Id, addedShow.Country.Id);
        //    Assert.Equal(existingCountry.Name, addedShow.Country.Name);
        //}

        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldThrowHttpRequestException_WhenGetAsyncThrows()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            // Configurar el HttpClient simulado para lanzar una HttpRequestException cuando se llama GetAsync
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ThrowsAsync(new HttpRequestException("Network error"))
               .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(_configuration["ApiUrl"])
            };

            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _showService.FetchAndStoreShowsAsync());

            // Verificar que el mensaje de la excepción es el esperado
            Assert.Contains("Error fetching shows from API.", exception.Message);
            Assert.IsType<HttpRequestException>(exception.InnerException);
            Assert.Equal("Network error", exception.InnerException.Message);

            // Verificar que SendAsync fue llamado exactamente una vez
            handlerMock.Protected().Verify(
               "SendAsync",
               Times.Once(),
               ItExpr.Is<HttpRequestMessage>(req =>
                   req.Method == HttpMethod.Get
                   && req.RequestUri == new Uri("http://api.tvmaze.com/shows")),
               ItExpr.IsAny<CancellationToken>()
            );

            // Verificar que no se intentaron agregar shows al repositorio
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.IsAny<Show>()), Times.Never);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente una respuesta vacía de la API.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_EmptyApiResponse()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>(); // Lista vacía

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.IsAny<Show>()), Times.Never);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        }

        /// <summary>
        /// Prueba que FetchAndStoreShowsAsync maneja correctamente shows con campos nulos en ShowDto.
        /// </summary>
        [Fact]
        public async Task FetchAndStoreShowsAsync_ShouldHandle_NullFieldsInShowDto()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var showsList = new List<ShowDto>
            {
                new ShowDto
                {
                    Id = 9,
                    Name = null, // Nombre es nulo
                    Language = null, // Idioma es nulo
                    Genres = new List<string> { null }, // Género nulo
                    Externals = null,
                    Rating = null,
                    Network = null
                }
            };

            var httpClient = CreateMockHttpClient(showsList);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(9)).ReturnsAsync((Show)null);
            _showRepositoryMock.Setup(repo => repo.AddShowAsync(It.IsAny<Show>())).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.FetchAndStoreShowsAsync();

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(It.Is<Show>(s => s.Id == 9 && s.Name == null)), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }



        /// <summary>
        /// Prueba que GetShowByIdAsync devuelve correctamente un show existente.
        /// </summary>
        [Fact]
        public async Task GetShowByIdAsync_ShouldReturnShow_WhenShowExists()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var show = new Show { Id = 1, Name = "Show 1" };
            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(1)).ReturnsAsync(show);

            // Act
            var result = await _showService.GetShowByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Show 1", result.Name);
        }

        /// <summary>
        /// Prueba que GetShowByIdAsync devuelve null cuando el show no existe.
        /// </summary>
        [Fact]
        public async Task GetShowByIdAsync_ShouldReturnNull_WhenShowDoesNotExist()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(1)).ReturnsAsync((Show)null);

            // Act
            var result = await _showService.GetShowByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Prueba que AddShowAsync agrega correctamente un nuevo show.
        /// </summary>
        [Fact]
        public async Task AddShowAsync_ShouldAddShow()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var show = new Show { Id = 1, Name = "New Show" };

            _showRepositoryMock.Setup(repo => repo.AddShowAsync(show)).Returns(Task.CompletedTask);
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.AddShowAsync(show);

            // Assert
            _showRepositoryMock.Verify(repo => repo.AddShowAsync(show), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que UpdateShowAsync actualiza correctamente un show existente.
        /// </summary>
        [Fact]
        public async Task UpdateShowAsync_ShouldUpdateShow()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var show = new Show { Id = 1, Name = "Updated Show" };

            _showRepositoryMock.Setup(repo => repo.UpdateShow(show)).Verifiable();
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.UpdateShowAsync(show);

            // Assert
            _showRepositoryMock.Verify(repo => repo.UpdateShow(show), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que DeleteShowAsync elimina correctamente un show existente.
        /// </summary>
        [Fact]
        public async Task DeleteShowAsync_ShouldDeleteShow()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            var show = new Show { Id = 1, Name = "Show to Delete" };

            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(1)).ReturnsAsync(show);
            _showRepositoryMock.Setup(repo => repo.DeleteShow(show)).Verifiable();
            _showRepositoryMock.Setup(repo => repo.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _showService.DeleteShowAsync(1);

            // Assert
            _showRepositoryMock.Verify(repo => repo.DeleteShow(show), Times.Once);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }

        /// <summary>
        /// Prueba que DeleteShowAsync no hace nada cuando el show no existe.
        /// </summary>
        [Fact]
        public async Task DeleteShowAsync_ShouldNotThrow_WhenShowDoesNotExist()
        {
            // Arrange
            InitializeService(useRealRepository: false); // Usa el repositorio simulado

            _showRepositoryMock.Setup(repo => repo.GetShowByIdAsync(1)).ReturnsAsync((Show)null);

            // Act
            await _showService.DeleteShowAsync(1);

            // Assert
            _showRepositoryMock.Verify(repo => repo.DeleteShow(It.IsAny<Show>()), Times.Never);
            _showRepositoryMock.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        }

        #endregion
    }
}
