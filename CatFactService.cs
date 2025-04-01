using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public record CatFactDto(string Fact, int Length);

public class CatFactService
{
    private readonly HttpClient _client;

    public CatFactService()
    {
        _client = new HttpClient();
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<CatFactDto> GetRandomCatFactAsync()
    {
        try
        {
            var response = await _client.GetAsync("https://catfact.ninja/fact");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("Получен пустой ответ от API.");
            }

            var catFact = JsonSerializer.Deserialize<JsonElement>(content);

            if (!catFact.TryGetProperty("fact", out var factProperty) || string.IsNullOrWhiteSpace(factProperty.GetString()))
            {
                throw new Exception("Ключ 'fact' отсутствует или содержит пустое значение в ответе API.");
            }

            if (!catFact.TryGetProperty("length", out var lengthProperty) || lengthProperty.ValueKind != JsonValueKind.Number)
            {
                throw new Exception("Ключ 'length' отсутствует или имеет недопустимое значение в ответе API.");
            }

            return new CatFactDto(
                Fact: factProperty.GetString(),
                Length: lengthProperty.GetInt32()
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка получения факта о кошках: {ex.Message}", ex);
        }
    }
}