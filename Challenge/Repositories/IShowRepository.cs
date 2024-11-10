using System.Collections.Generic;
using System.Threading.Tasks;
using Challenge.DTOs;
using Challenge.Models;

namespace Challenge.Repositories
{
    public interface IShowRepository
    {
        Task<IEnumerable<Show>> GetAllShowsAsync();
        Task<Show> GetShowByIdAsync(int id);
        Task AddShowAsync(Show show);
        void UpdateShow(Show show);
        void DeleteShow(Show show);
        Task SaveChangesAsync();
    }
}
