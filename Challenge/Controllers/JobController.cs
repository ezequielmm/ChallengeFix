using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Challenge.Services;

namespace Challenge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IShowService _showService;
        private readonly IConfiguration _configuration;

        public JobController(IShowService showService, IConfiguration configuration)
        {
            _showService = showService;
            _configuration = configuration;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunJob([FromHeader(Name = "x-api-key")] string apiKey)
        {
            var configuredApiKey = _configuration["ApiKey"];
            if (apiKey != configuredApiKey)
            {
                return Unauthorized("Invalid API key.");
            }

            try
            {
                await _showService.FetchAndStoreShowsAsync();
                return Ok("Job executed successfully.");
            }
            catch (Exception ex)
            {
                // Aquí podrías agregar registro de errores si es necesario
                return StatusCode(500, "An error occurred while executing the job.");
            }
        }
    }
}
