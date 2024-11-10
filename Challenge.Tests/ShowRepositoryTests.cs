using Xunit;
using Microsoft.EntityFrameworkCore;
using Challenge.Data;
using Challenge.Models;
using Challenge.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Challenge.Tests.Repositories
{
    public class ShowRepositoryTests
    {
        private readonly DbContextOptions<ApplicationDbContext> _dbContextOptions;
        private ApplicationDbContext _context;
        private ShowRepository _showRepository;

        public ShowRepositoryTests()
        {
            // Configuración de la base de datos en memoria
            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ShowRepositoryTests")
                .Options;
        }

        private void InitializeRepository()
        {
            // Inicializa el contexto y el repositorio antes de cada prueba
            _context = new ApplicationDbContext(_dbContextOptions);
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
            _showRepository = new ShowRepository(_context);
        }

        [Fact]
        public async Task GetAllShowsAsync_ShouldReturnAllShows()
        {
            // Arrange
            InitializeRepository();

            var shows = new List<Show>
            {
                new Show { Id = 1, Name = "Show 1" },
                new Show { Id = 2, Name = "Show 2" }
            };

            _context.Shows.AddRange(shows);
            await _context.SaveChangesAsync();

            // Act
            var result = await _showRepository.GetAllShowsAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, s => s.Name == "Show 1");
            Assert.Contains(result, s => s.Name == "Show 2");
        }

        [Fact]
        public async Task GetShowByIdAsync_ShouldReturnShow_WhenShowExists()
        {
            // Arrange
            InitializeRepository();

            var show = new Show { Id = 1, Name = "Show 1" };
            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            // Act
            var result = await _showRepository.GetShowByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Show 1", result.Name);
        }

        [Fact]
        public async Task GetShowByIdAsync_ShouldReturnNull_WhenShowDoesNotExist()
        {
            // Arrange
            InitializeRepository();

            // Act
            var result = await _showRepository.GetShowByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddShowAsync_ShouldAddShow()
        {
            // Arrange
            InitializeRepository();

            var show = new Show { Id = 1, Name = "New Show" };

            // Act
            await _showRepository.AddShowAsync(show);
            await _showRepository.SaveChangesAsync();

            // Assert
            var showsInDb = await _context.Shows.ToListAsync();
            Assert.Single(showsInDb);
            Assert.Equal("New Show", showsInDb.First().Name);
        }

        [Fact]
        public async Task UpdateShow_ShouldUpdateShow()
        {
            // Arrange
            InitializeRepository();

            var show = new Show { Id = 1, Name = "Original Show" };
            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            // Act
            show.Name = "Updated Show";
            _showRepository.UpdateShow(show);
            await _showRepository.SaveChangesAsync();

            // Assert
            var updatedShow = await _context.Shows.FindAsync(1);
            Assert.NotNull(updatedShow);
            Assert.Equal("Updated Show", updatedShow.Name);
        }

        [Fact]
        public async Task DeleteShow_ShouldDeleteShow()
        {
            // Arrange
            InitializeRepository();

            var show = new Show { Id = 1, Name = "Show to Delete" };
            _context.Shows.Add(show);
            await _context.SaveChangesAsync();

            // Act
            _showRepository.DeleteShow(show);
            await _showRepository.SaveChangesAsync();

            // Assert
            var deletedShow = await _context.Shows.FindAsync(1);
            Assert.Null(deletedShow);
        }
    }
}
