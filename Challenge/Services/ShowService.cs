using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Challenge.Data;
using Challenge.DTOs;
using Challenge.Models;
using Challenge.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Challenge.DTO;

namespace Challenge.Services
{
    public class ShowService : IShowService
    {
        private readonly IShowRepository _showRepository;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiUrl;

        public ShowService(
            IShowRepository showRepository,
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _showRepository = showRepository ?? throw new ArgumentNullException(nameof(showRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

            _apiUrl = configuration?["ApiUrl"] ?? throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrWhiteSpace(_apiUrl))
                throw new ArgumentException("ApiUrl configuration is missing or empty.", nameof(configuration));
        }

        public async Task FetchAndStoreShowsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(_apiUrl);

            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync("shows");
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException("Error fetching shows from API.", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Failed to fetch shows from API.");
            }

            var content = await response.Content.ReadAsStringAsync();
            var showsDto = JsonSerializer.Deserialize<List<ShowDto>>(content, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            bool changesMade = false;

            foreach (var showDto in showsDto)
            {
                if (await _showRepository.GetShowByIdAsync(showDto.Id) != null)
                {
                    continue;
                }

                var show = new Show
                {
                    Id = showDto.Id,
                    Name = showDto.Name,
                    Language = showDto.Language,
                    Genres = new List<Genre>()
                };

                await AddGenresToShowAsync(show, showDto.Genres);

                if (showDto.Externals != null)
                {
                    show.Externals = new Externals
                    {
                        Imdb = showDto.Externals.Imdb,
                        Tvrage = showDto.Externals.Tvrage,
                        Thetvdb = showDto.Externals.Thetvdb
                    };
                }

                if (showDto.Rating != null)
                {
                    show.Rating = new Rating
                    {
                        Average = showDto.Rating.Average
                    };
                }

                if (showDto.Network != null)
                {
                    show.Network = await GetOrCreateNetworkAsync(showDto.Network);
                }

                await _showRepository.AddShowAsync(show);
                changesMade = true;
            }

            if (changesMade)
            {
                await _showRepository.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Show>> GetAllShowsAsync()
        {
            return await _showRepository.GetAllShowsAsync();
        }

        public async Task<Show> GetShowByIdAsync(int id)
        {
            return await _showRepository.GetShowByIdAsync(id);
        }

        public async Task AddShowAsync(Show show)
        {
            await _showRepository.AddShowAsync(show);
            await _showRepository.SaveChangesAsync();
        }

        public async Task UpdateShowAsync(Show show)
        {
            _showRepository.UpdateShow(show);
            await _showRepository.SaveChangesAsync();
        }

        public async Task DeleteShowAsync(int id)
        {
            var show = await _showRepository.GetShowByIdAsync(id);
            if (show != null)
            {
                _showRepository.DeleteShow(show);
                await _showRepository.SaveChangesAsync();
            }
        }

        private async Task AddGenresToShowAsync(Show show, List<string> genres)
        {
            if (genres == null) return;

            foreach (var genreName in genres)
            {
                if (string.IsNullOrWhiteSpace(genreName)) continue;

                var genre = _context.Genres.Local.FirstOrDefault(g => g.Name == genreName)
                             ?? await _context.Genres.FirstOrDefaultAsync(g => g.Name == genreName);
                if (genre == null)
                {
                    genre = new Genre { Name = genreName };
                    _context.Genres.Add(genre);
                }
                show.Genres.Add(genre);
            }
        }

        private async Task<Network> GetOrCreateNetworkAsync(NetworkDto networkDto)
        {
            var existingNetwork = _context.Networks.Local.FirstOrDefault(n => n.Id == networkDto.Id)
                                  ?? await _context.Networks.FirstOrDefaultAsync(n => n.Id == networkDto.Id);

            if (existingNetwork != null)
            {
                return existingNetwork;
            }

            var newNetwork = new Network
            {
                Id = networkDto.Id,
                Name = networkDto.Name,
                Country = networkDto.Country != null ? await GetOrCreateCountryAsync(networkDto.Country) : null
            };

            _context.Networks.Add(newNetwork);
            return newNetwork;
        }

        private async Task<Country> GetOrCreateCountryAsync(CountryDto countryDto)
        {
            var existingCountry = _context.Countries.Local.FirstOrDefault(c => c.Id == countryDto.Id)
                                 ?? await _context.Countries.FirstOrDefaultAsync(c => c.Id == countryDto.Id);

            if (existingCountry != null)
            {
                return existingCountry;
            }

            var country = new Country
            {
                Id = countryDto.Id,
                Code = countryDto.Code,
                Name = countryDto.Name,
                Timezone = countryDto.Timezone
            };

            _context.Countries.Add(country);
            return country;
        }
    }
}
