using Discord.WebSocket;
using DiscordBotNightOwl.Data;

namespace DiscordBotNightOwl.Services
{
    public class VoiceStateService
    {
        private readonly IServiceProvider _services;

        public VoiceStateService(IServiceProvider services)
        {
            _services = services;
        }

        public async Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            if (user.IsBot) return;

            // CRITICAL CHECK: Ignore if just Mute/Unmute (same channel ID)
            if (oldState.VoiceChannel?.Id == newState.VoiceChannel?.Id) return;

            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BotContext>();

                // --- PART 1: HANDLE LEAVE (CHECK-OUT) ---
                if (oldState.VoiceChannel != null)
                {
                    var lastSession = db.VoiceSessions
                        .OrderByDescending(x => x.JoinTime)
                        .FirstOrDefault(x => x.UserId == user.Id && x.LeaveTime == null);

                    if (lastSession != null)
                    {
                        lastSession.LeaveTime = DateTime.Now;
                        // Calculate duration
                        lastSession.DurationMinutes = (lastSession.LeaveTime.Value - lastSession.JoinTime).TotalMinutes;

                        await db.SaveChangesAsync();
                        Console.WriteLine($"[Voice] {user.Username} left {oldState.VoiceChannel.Name}. Total: {lastSession.DurationMinutes:F2} mins");
                    }
                }

                // --- PART 2: HANDLE JOIN (CHECK-IN) ---
                if (newState.VoiceChannel != null)
                {
                    var session = new VoiceSession
                    {
                        GuildId = newState.VoiceChannel.Guild.Id,
                        UserId = user.Id,
                        GuildName = newState.VoiceChannel.Guild.Name,
                        ChannelName = newState.VoiceChannel.Name,
                        UserName = user.Username,
                        JoinTime = DateTime.Now,
                        DurationMinutes = 0
                    };

                    db.VoiceSessions.Add(session);
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[Voice] {user.Username} joined channel: {newState.VoiceChannel.Name}");
                }
            }
        }
    }
}