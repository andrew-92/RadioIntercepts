using System.Net.Http;

namespace RadioIntercepts.Application.Parsers.Sources
{
    public class WebServiceRadioMessageSource : IRadioMessageSource
    {
        private readonly HttpClient _httpClient;

        public WebServiceRadioMessageSource(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetRawMessageAsync()
        {
            // Запрос к твоему API
            var response = await _httpClient.GetAsync("https://api.example.com/radiomessage");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
