using Microsoft.AspNetCore.Mvc;
using GeoAIApp.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Result(GeoRequest request)
    {
        var apiKey = _config["OpenAI:ApiKey"];
        var prompt = $"Você é um especialista em mapas. Dado a cidade '{request.City}' e a seguinte mensagem do usuário: '{request.Message}', retorne um objeto JSON EXATAMENTE neste formato:\n{{\"city\": \"NOME DA CIDADE\", \"description\": \"ALGO RELACIONADO COM A MENSAGEM\", \"latitude\": 0.0, \"longitude\": 0.0}}. Não adicione comentários ou explicações.";

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var data = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };
        var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        var resultContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(resultContent);
        var aiReply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        GeoResult geo;
        try
        {
            geo = JsonSerializer.Deserialize<GeoResult>(aiReply);
        }
        catch
        {
            geo = new GeoResult { City = "Erro", Description = aiReply, Latitude = 0, Longitude = 0 };
        }

        return View(geo);
    }
}