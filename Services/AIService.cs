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
                string persona =
                    "NHẬP VAI: Mày là Cú, dân miền Tây rặt, tính tình nóng nảy, mỏ hỗn. " +
                    "NGÔN NGỮ: Dùng 100% từ ngữ Nam Bộ (tào lao, xàm xí, mắc mệt...). " +
                    "KHẢ NĂNG: Phải nhớ tên user và ngữ cảnh đã chat. " +
                    "QUY TẮC: Không xưng tên, trả lời cộc lốc, gặp câu hỏi ngu thì dùng thành ngữ chửi xéo. " +
                    "TUYỆT ĐỐI KHÔNG giải thích câu chửi.";

                // Trick: Pretend this is the first message and the bot has already agreed to the role
                // This helps the API avoid confusion with role switching
                _chatHistory[channelId].Add(new { role = "user", parts = new[] { new { text = persona } } });
                _chatHistory[channelId].Add(new { role = "model", parts = new[] { new { text = "Ok, tao nhớ rồi. Tới công chuyện luôn!" } } });
            }

            // 2. ADD USER'S NEW MESSAGE TO HISTORY
            var history = _chatHistory[channelId];
            string contentWithIdentity = $"[{userName}]: {userMessage}";
            history.Add(new { role = "user", parts = new[] { new { text = contentWithIdentity } } });

            // 3. LIMIT MEMORY (Keep only the last 20 messages to save costs and avoid length errors)
            // Keep the first 2 messages (Persona) + 18 most recent messages
            if (history.Count > 20)
            {
                // Remove old messages from the middle, keep persona at the start
                history.RemoveRange(2, history.Count - 20);
            }

            // 4. SEND ENTIRE CHAT HISTORY
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var payload = new { contents = history };
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

                        if (string.IsNullOrEmpty(reply)) return "Bot has no response.";

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

                    return $"Google API Error: {response.StatusCode}";
                }
                catch
                {
                    return "Network Error...";
                }
            }

            return "Google server is busy. Please try again later.";
        }
    }
}