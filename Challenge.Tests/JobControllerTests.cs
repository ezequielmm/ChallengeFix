using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Challenge.Services;
using Challenge.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Challenge.Tests.Controllers
{
    public class JobControllerTests
    {
        private readonly Mock<IShowService> _showServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly JobController _controller;

        public JobControllerTests()
        {
            _showServiceMock = new Mock<IShowService>();
            _configurationMock = new Mock<IConfiguration>();
            _controller = new JobController(_showServiceMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task RunJob_ReturnsUnauthorized_WhenApiKeyIsInvalid()
        {
            // Arrange
            _configurationMock.Setup(c => c["ApiKey"]).Returns("valid-api-key");
            var invalidApiKey = "invalid-api-key";

            // Act
            var result = await _controller.RunJob(invalidApiKey);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.Equal("Invalid API key.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task RunJob_ReturnsOk_WhenApiKeyIsValid()
        {
            // Arrange
            _configurationMock.Setup(c => c["ApiKey"]).Returns("valid-api-key");
            var validApiKey = "valid-api-key";

            // Act
            var result = await _controller.RunJob(validApiKey);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.Equal("Job executed successfully.", okResult.Value);
            _showServiceMock.Verify(s => s.FetchAndStoreShowsAsync(), Times.Once);
        }

        [Fact]
        public async Task RunJob_ReturnsInternalServerError_WhenExceptionIsThrown()
        {
            // Arrange
            _configurationMock.Setup(c => c["ApiKey"]).Returns("valid-api-key");
            _showServiceMock.Setup(s => s.FetchAndStoreShowsAsync()).ThrowsAsync(new System.Exception("Service error"));

            // Act
            var result = await _controller.RunJob("valid-api-key");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Equal("An error occurred while executing the job.", objectResult.Value);
        }

        [Fact]
        public async Task RunJob_ReturnsBadRequest_WhenApiKeyIsNull()
        {
            // Arrange
            _configurationMock.Setup(c => c["ApiKey"]).Returns("valid-api-key");

            // Act
            var result = await _controller.RunJob(null);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.Equal("Invalid API key.", unauthorizedResult.Value);
        }
    }
}
