using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DiscordBotNightOwl.Services
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private static readonly ConcurrentDictionary<ulong, List<dynamic>> _chatHistory = new();

        public AIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> AskGeminiAsync(ulong channelId, string userName, string userMessage)
        {
            string? apiKey = _configuration["Gemini:Key"];
            string? modelName = _configuration["Gemini:Model"];

            if (string.IsNullOrEmpty(apiKey)) return "Error: Missing API Key.";
            if (string.IsNullOrEmpty(modelName)) modelName = "gemma-3-27b-it";

            modelName = modelName.Trim();
            apiKey = apiKey.Trim();

            // 1. INITIALIZE CHAT HISTORY (If this channel hasn't chatted before)
            if (!_chatHistory.ContainsKey(channelId))
            {
                _chatHistory[channelId] = new List<dynamic>();

                // Load persona at the beginning (Only done once)
                string personaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "BotPersona.txt");
                string persona = File.Exists(personaPath)
                                 ? File.ReadAllText(personaPath)
                                 : "Tui là bot Cú đêm :v";

                _chatHistory[channelId].Add(new { role = "user", parts = new[] { new { text = persona } } });
                _chatHistory[channelId].Add(new { role = "model", parts = new[] { new { text = "Ok!" } } });
            }

            // 2. ADD USER'S NEW MESSAGE TO HISTORY
            var history = _chatHistory[channelId];
            string contentWithIdentity = $"[{userName}]: {userMessage}";
            history.Add(new { role = "user", parts = new[] { new { text = contentWithIdentity } } });

            // 3. LIMIT MEMORY (Keep only the last 20 messages to save costs and avoid length errors)
            if (history.Count > 20)
            {
                history.RemoveRange(2, history.Count - 20);
            }

            // 4. SEND ENTIRE CHAT HISTORY
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            //Current timme
            string currentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            string systemPrompt = $"Hôm nay là {currentTime}.";

            var payload = new
            {
                systemInstruction = new
                {
                    part = new[] { new { text = systemPrompt } }
                },
                contents = history
            };
            string jsonString = JsonSerializer.Serialize(payload);

            int maxRetries = 3;
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                try
                {
                    var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, jsonContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var jsonNode = JsonNode.Parse(responseString);
                        string? reply = jsonNode?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                        if (string.IsNullOrEmpty(reply)) return "Từ từ... não t chưa load được, nãy m nói gì á?";

                        // 5. SAVE BOT'S RESPONSE TO HISTORY (So it remembers what it said for next time)
                        history.Add(new { role = "model", parts = new[] { new { text = reply } } });

                        return reply;
                    }

                    if ((int)response.StatusCode == 429) // Rate limited
                    {
                        currentRetry++;
                        await Task.Delay(2000 * currentRetry);
                        continue;
                    }

                    //return $"Google API Error: {response.StatusCode}";
                    return "Hình như GoogleAI bị lỗi rồi, thử nhắn lại giùm tui nha.";
                }
                catch
                {
                    return "Ôi cái internet trên sao Hỏa... nãy ní nhắn gì á, tui không đọc được :v";
                }
            }

            return "...";
        }
    }
}