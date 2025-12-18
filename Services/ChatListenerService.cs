using Discord;
using Discord.WebSocket;
using DiscordBotNightOwl.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscordBotNightOwl.Services
{
    public class ChatListenerService
    {
        private readonly IServiceProvider _services;
        private readonly AIService _aiService;
        private readonly IConfiguration _config;

        public ChatListenerService(IServiceProvider services, AIService aiService, IConfiguration config)
        {
            _services = services;
            _aiService = aiService;
            _config = config;
        }

        public Task OnMessageReceived(SocketMessage message)
        {
            // 1. Quick Validation
            if (message.Author.IsBot || string.IsNullOrEmpty(message.Content)) return Task.CompletedTask;

            // 2. OFFLOAD TO BACKGROUND TASK
            _ = Task.Run(async () =>
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<BotContext>();
                        var user = message.Author as SocketGuildUser;
                        if (user == null) return;

                        var setting = await db.GuildSettings.FindAsync(user.Guild.Id);
                        if (setting == null || setting.AIChannelId != message.Channel.Id) return;

                        string aiType = _config["RoleTypes:AI"] ?? "AI_ACCESS";
                        var allowedRoleIds = await db.GuildRoleConfigs
                            .Where(x => x.GuildId == user.Guild.Id && x.ConfigType == aiType)
                            .Select(x => x.RoleId)
                            .ToListAsync();

                        // --- CALL AI ---
                        if (user.Roles.Any(r => allowedRoleIds.Contains(r.Id)))
                        {
                            using (message.Channel.EnterTypingState())
                            {
                                string name = message.Author.GlobalName ?? message.Author.Username;

                                string answer = await _aiService.AskGeminiAsync(message.Channel.Id, name, message.Content);

                                if (answer.Length > 2000) answer = answer.Substring(0, 1990) + "...";

                                // Reply
                                await message.Channel.SendMessageAsync(answer, messageReference: new MessageReference(message.Id));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error in ChatListener] {ex.Message}");
                }
            });

            return Task.CompletedTask;
        }
    }
}