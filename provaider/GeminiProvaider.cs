using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AVIA_Bot.Provaider
{
    public class GeminiProvaider : IAIProvider
    {
        public string Name => "Gemini";

        private string ApiKey { get; set; }

        public GeminiProvaider(string apiKey)
        {
            this.ApiKey = apiKey;
        }

        public async Task<string> ResponseAsync(List<(string role, string text)> messages)
        {
            using var httpClient = new HttpClient();
            string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={this.ApiKey}";

            var content = messages.Select(item => new
            {
                role = item.role,
                parts = new[] { new { text = item.text } }
            }).ToList();

            var requestBody = new { contents = content };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, jsonContent);
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error API Response: {response.StatusCode}\n{responseData}";
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(responseData);
                var resultText = jsonDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return resultText ?? "Gemini returned an empty answer.";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}\nAPI response:\n{responseData}";
            }
        }
    }
}