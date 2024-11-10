using System.Collections.Generic;
using System.Threading.Tasks;
using Challenge.Models;

namespace Challenge.Services
{
    public interface IShowService
    {
        Task FetchAndStoreShowsAsync();
        Task<IEnumerable<Show>> GetAllShowsAsync();
        Task<Show> GetShowByIdAsync(int id);
        Task AddShowAsync(Show show);
        Task UpdateShowAsync(Show show);
        Task DeleteShowAsync(int id);
    }
}
