using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Challenge.Services;
using Challenge.Models;

namespace Challenge.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShowsController : ControllerBase
    {
        private readonly IShowService _showService;

        public ShowsController(IShowService showService)
        {
            _showService = showService;
        }

        // GET: api/shows
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Show>>> GetAllShows()
        {
            var shows = await _showService.GetAllShowsAsync();
            return Ok(shows);
        }

        // GET: api/shows/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Show>> GetShowById(int id)
        {
            var show = await _showService.GetShowByIdAsync(id);

            if (show == null)
            {
                return NotFound();
            }

            return Ok(show);
        }

        // POST: api/shows
        [HttpPost]
        public async Task<ActionResult<Show>> CreateShow([FromBody] Show show)
        {
            if (show == null)
            {
                return BadRequest("Show cannot be null.");
            }

            await _showService.AddShowAsync(show);
            return CreatedAtAction(nameof(GetShowById), new { id = show.Id }, show);
        }

        // PUT: api/shows/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShow(int id, [FromBody] Show show)
        {
            if (show == null || id != show.Id)
            {
                return BadRequest("Show data is invalid.");
            }

            var existingShow = await _showService.GetShowByIdAsync(id);

            if (existingShow == null)
            {
                return NotFound();
            }

            await _showService.UpdateShowAsync(show);
            return NoContent();
        }

        // DELETE: api/shows/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShow(int id)
        {
            var existingShow = await _showService.GetShowByIdAsync(id);

            if (existingShow == null)
            {
                return NotFound();
            }

            await _showService.DeleteShowAsync(id);
            return NoContent();
        }
    }
}
