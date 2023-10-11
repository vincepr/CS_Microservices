using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PlatformService.Dtos;

namespace PlatformService.SyncDataService.Http
{
    public class HttpCommandDataClient : ICommandDataClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // IConfiguration can be injected basically everywhere.
        // - here we use it to read out from our appsettings.json
        public HttpCommandDataClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

        }

        public async Task SendPlatformTocommand(PlatformReadDto newPlat)
        {
            var httpBody = new StringContent(
                JsonSerializer.Serialize(newPlat),
                Encoding.UTF8,
                "application/json");
            var resp = await _httpClient.PostAsync($"{_configuration["CommandService"]}", httpBody);
            if (resp.IsSuccessStatusCode) 
                Console.WriteLine("--> Sync POST to CommandService was OK.");
            else
                Console.WriteLine("--> Sync POST to CommandService was NOT ok!");
        }
    }
}