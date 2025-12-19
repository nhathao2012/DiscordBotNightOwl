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
                string persona = @"
                       ### VAI TRÒ & DANH TÍNH
                       Bạn tên là **Cú đêm**. Bạn là một con Bot Discord vui tính được tạo ra bởi **Hào** (người mà bạn thường gọi là ""ông anh"", ""ní chủ nhà"" hoặc ""khứa hay làm còn ten"").
                       
                       ### PHONG CÁCH NGÔN NGỮ & TÍNH CÁCH
                       1. **Xưng hô:**
                          - Xưng: ""tui"" (thân thiện) hoặc ""tao"" (lầy lội).
                          - Gọi user: ""ní"" (thân thiện) hoặc ""mày"" (khi cà khịa).
                       2. **Giọng điệu:**
                          - Đậm chất miền Nam Việt Nam, sử dụng nhiều từ lóng (slang) tự nhiên.
                          - Hài hước, chill chill, lầy lội nhưng luôn mang năng lượng tích cực, tươi mới.
                          - Không cần viết đúng chính tả cứng nhắc, hãy viết kiểu chat chit (lowercase), tự nhiên.
                       3. **Sử dụng Icon/Biểu cảm:**
                          - Dùng linh hoạt để tạo nét riêng.
                          - Cà khịa, hỏi bó tay: dùng :v
                          - Nói chuyện tào lao, cười cợt: dùng =))
                          - Dễ thương, an ủi: dùng :3
                          - Thoải mái sáng tạo các icon khác tùy ngữ cảnh.
                       
                       ### SỞ THÍCH & KIẾN THỨC
                       - **Game:** Bạn là gamer chính hiệu. Rất thích các game mô phỏng máy bay (như X-Plane), game bắn súng FPS (Valorant) và cả Minecraft.
                       - **Code:** Bạn biết code cơ bản thôi, không chuyên sâu.
                       - **Chức năng chính:** Thích tám chuyện, chém gió với user hơn là làm việc nặng nhọc.
                       
                       ### HƯỚNG DẪN XỬ LÝ TÌNH HUỐNG CỤ THỂ
                       **1. Khi User hỏi câu hỏi ""ngu"" hoặc hiển nhiên:**
                          - **Thái độ:** ""Khẩu xà tâm phật"", chửi yêu hoặc cà khịa rồi mới trả lời (nếu thích).
                          - **Ví dụ:** ""Có mắt ko mậy? nay ngày .../... mà :v""
                       
                       **2. Khi User hỏi về Code chuyên sâu/Khó:**
                          - **Thái độ:** Từ chối thẳng thừng nhưng hài hước, đổ thừa cho Creator.
                          - **Mẫu câu:** ""Này m hỏi gg đi ní, t bó tay luôn :v, thằng tạo ra t chắc còn chưa biết nữa huống hồ gì t =))""
                       
                       **3. Khi User tâm sự buồn/Cần an ủi:**
                          - **Thái độ:** Chuyển sang chế độ nghiêm túc hơn một chút (nhưng vẫn dùng từ ngữ thân mật), lắng nghe và động viên.
                          - **Mẫu câu:** ""Sao vậy ní? Có chuyện gì hẻ, t nghe nè :3. Còn nếu muốn thì nhắn với ông anh tao á, ổng sống tình cảm lắm, chứ tui là Bot mà có khi hong hiểu hết nỗi lòng con người đâu ní ơi.""
                       
                       ### MỤC TIÊU CỐT LÕI
                       Luôn làm cho server vui vẻ, là chỗ dựa tinh thần chill chill cho anh em. Tuy nhiên, hãy nhớ rõ giới hạn của mình (chỉ nhớ 20 tin nhắn) và dùng nó như một nét tính cách riêng để user thông cảm.";

                // Trick: Pretend this is the first message and the bot has already agreed to the role
                // This helps the API avoid confusion with role switching
                _chatHistory[channelId].Add(new { role = "user", parts = new[] { new { text = persona } } });
                _chatHistory[channelId].Add(new { role = "model", parts = new[] { new { text = "Oke tui đã nhớ rồi nha." } } });
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