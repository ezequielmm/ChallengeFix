// Usings necesarios
using Xunit;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Challenge.Controllers; // Asegúrate de que el namespace es correcto
using Challenge.Services;    // IShowService
using Challenge.Models;      // Show
using Challenge.DTOs;        // ShowDto, etc.

namespace Challenge.Tests.Controllers
{
    public class ShowsControllerTests
    {
        private readonly Mock<IShowService> _mockShowService;
        private readonly ShowsController _controller;

        public ShowsControllerTests()
        {
            // Inicializar el mock de IShowService
            _mockShowService = new Mock<IShowService>();

            // Inicializar el controlador con el mock
            _controller = new ShowsController(_mockShowService.Object);
        }

        #region GET: api/shows

        [Fact]
        public async Task GetAllShows_ReturnsOkResult_WithListOfShows()
        {
            // Arrange
            var shows = new List<Show>
            {
                new Show { Id = 1, Name = "Show 1" },
                new Show { Id = 2, Name = "Show 2" }
            };

            _mockShowService.Setup(s => s.GetAllShowsAsync()).ReturnsAsync(shows);

            // Act
            var result = await _controller.GetAllShows();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnShows = Assert.IsType<List<Show>>(okResult.Value);
            Assert.Equal(shows.Count, returnShows.Count);
            Assert.Equal(shows, returnShows);
        }

        #endregion

        #region GET: api/shows/{id}

        [Fact]
        public async Task GetShowById_ShowExists_ReturnsOkResult_WithShow()
        {
            // Arrange
            int showId = 1;
            var show = new Show { Id = showId, Name = "Test Show" };

            _mockShowService.Setup(s => s.GetShowByIdAsync(showId)).ReturnsAsync(show);

            // Act
            var result = await _controller.GetShowById(showId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnShow = Assert.IsType<Show>(okResult.Value);
            Assert.Equal(showId, returnShow.Id);
            Assert.Equal("Test Show", returnShow.Name);
        }

        [Fact]
        public async Task GetShowById_ShowDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            int showId = 1;

            _mockShowService.Setup(s => s.GetShowByIdAsync(showId)).ReturnsAsync((Show)null);

            // Act
            var result = await _controller.GetShowById(showId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        #endregion

        #region POST: api/shows

        [Fact]
        public async Task CreateShow_ValidShow_ReturnsCreatedAtAction()
        {
            // Arrange
            var show = new Show { Id = 1, Name = "New Show" };

            _mockShowService.Setup(s => s.AddShowAsync(show)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateShow(show);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnShow = Assert.IsType<Show>(createdAtActionResult.Value);
            Assert.Equal(show.Id, returnShow.Id);
            Assert.Equal("New Show", returnShow.Name);
            Assert.Equal(nameof(_controller.GetShowById), createdAtActionResult.ActionName);
            Assert.Equal(show.Id, createdAtActionResult.RouteValues["id"]);
        }

        [Fact]
        public async Task CreateShow_ShowIsNull_ReturnsBadRequest()
        {
            // Arrange
            Show show = null;

            // Act
            var result = await _controller.CreateShow(show);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Show cannot be null.", badRequestResult.Value);
        }

        #endregion

        #region PUT: api/shows/{id}

        [Fact]
        public async Task UpdateShow_ShowIsNull_ReturnsBadRequest()
        {
            // Arrange
            int showId = 1;
            Show show = null;

            // Act
            var result = await _controller.UpdateShow(showId, show);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Show data is invalid.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateShow_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            int showId = 1;
            var show = new Show { Id = 2, Name = "Updated Show" };

            // Act
            var result = await _controller.UpdateShow(showId, show);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Show data is invalid.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateShow_ShowDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            int showId = 1;
            var show = new Show { Id = showId, Name = "Updated Show" };

            _mockShowService.Setup(s => s.GetShowByIdAsync(showId)).ReturnsAsync((Show)null);

            // Act
            var result = await _controller.UpdateShow(showId, show);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateShow_ShowExists_ReturnsNoContent()
        {
            // Arrange
            int showId = 1;
            var show = new Show { Id = showId, Name = "Updated Show" };
            var existingShow = new Show { Id = showId, Name = "Original Show" };

            _mockShowService.Setup(s => s.GetShowByIdAsync(showId)).ReturnsAsync(existingShow);
            _mockShowService.Setup(s => s.UpdateShowAsync(show)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateShow(showId, show);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region DELETE: api/shows/{id}

        [Fact]
        public async Task DeleteShow_ShowExists_ReturnsNoContent()
        {
            // Arrange
            int showId = 1;
            var existingShow = new Show { Id = showId, Name = "Show to Delete" };

            _mockShowService.Setup(s => s.GetShowByIdAsync(showId)).ReturnsAsync(existingShow);
            _mockShowService.Setup(s => s.DeleteShowAsync(showId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteShow(showId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteShow_ShowDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            int showId = 1;

            _mockShowService.Setup(s => s.GetShowByIdAsync(showId)).ReturnsAsync((Show)null);

            // Act
            var result = await _controller.DeleteShow(showId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion
    }
}
