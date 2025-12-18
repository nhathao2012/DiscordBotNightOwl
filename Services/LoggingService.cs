using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBotNightOwl.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscordBotNightOwl.Services
{
    public class LoggingService
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _client;

        public LoggingService(IServiceProvider services, DiscordSocketClient client)
        {
            _services = services;
            _client = client;
        }

        // --- 1. HANDLE VOICE EVENTS (Move, Disconnect, Mute) ---
        public async Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            if (user.IsBot) return;
            var guildUser = user as SocketGuildUser;
            if (guildUser == null) return;

            var logChannel = await GetLogChannelAsync(guildUser.Guild.Id);
            if (logChannel == null) return;

            // A. VOICE CHANNEL MOVE
            if (oldState.VoiceChannel != null && newState.VoiceChannel != null && oldState.VoiceChannel.Id != newState.VoiceChannel.Id)
            {
                // Cast to type 26 (MemberMove) to avoid missing Enum error
                var entry = await GetAuditLogAsync(guildUser.Guild, (ActionType)26, user.Id);

                if (entry != null && entry.User.Id != user.Id)
                {
                    await logChannel.SendMessageAsync($"**VOICE MOVE**\nExecutor: {entry.User.Mention}\nTarget: {user.Mention}\nFrom: `{oldState.VoiceChannel.Name}` -> To: `{newState.VoiceChannel.Name}`");
                }
            }

            // B. VOICE CHANNEL DISCONNECT
            if (oldState.VoiceChannel != null && newState.VoiceChannel == null)
            {
                // Cast to type 27 (MemberDisconnect)
                var entry = await GetAuditLogAsync(guildUser.Guild, (ActionType)27, user.Id);

                if (entry != null && entry.User.Id != user.Id)
                {
                    await logChannel.SendMessageAsync($"**VOICE KICK**\nExecutor: {entry.User.Mention}\nTarget: {user.Mention}\nKicked from: `{oldState.VoiceChannel.Name}`");
                }
            }

            // C. SERVER MUTE
            if (!oldState.IsMuted && newState.IsMuted)
            {
                var entry = await GetAuditLogAsync(guildUser.Guild, ActionType.MemberUpdated, user.Id);
                if (entry != null && entry.User.Id != user.Id)
                {
                    await logChannel.SendMessageAsync($"**SERVER MUTE**\nExecutor: {entry.User.Mention}\nTarget: {user.Mention}\nMicrophone has been muted.");
                }
            }
        }

        // --- 2. HANDLE TIMEOUT ---
        public async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> oldUser, SocketGuildUser newUser)
        {
            var logChannel = await GetLogChannelAsync(newUser.Guild.Id);
            if (logChannel == null) return;

            var oldUserValue = await oldUser.GetOrDownloadAsync();

            if (newUser.TimedOutUntil.HasValue && (oldUserValue == null || !oldUserValue.TimedOutUntil.HasValue))
            {
                var entry = await GetAuditLogAsync(newUser.Guild, ActionType.MemberUpdated, newUser.Id);
                string executor = entry != null ? entry.User.Mention : "Unknown";

                await logChannel.SendMessageAsync($"**TIMEOUT**\nExecutor: {executor}\nTarget: {newUser.Mention}\nExpires: {newUser.TimedOutUntil.Value.ToString("dd/MM/yyyy HH:mm")}");
            }
        }

        // --- 3. HANDLE BAN ---
        public async Task OnUserBanned(SocketUser user, SocketGuild guild)
        {
            var logChannel = await GetLogChannelAsync(guild.Id);
            if (logChannel == null) return;

            var entry = await GetAuditLogAsync(guild, ActionType.Ban, user.Id);
            string executor = entry != null ? entry.User.Mention : "Unknown";
            string reason = entry != null ? entry.Reason : "No reason provided";

            await logChannel.SendMessageAsync($"**BAN**\nExecutor: {executor}\nTarget: {user.Username} (ID: {user.Id})\nReason: {reason}");
        }

        // --- 4. HANDLE KICK ---
        public async Task OnUserLeft(SocketGuild guild, SocketUser user)
        {
            var logChannel = await GetLogChannelAsync(guild.Id);
            if (logChannel == null) return;

            var entry = await GetAuditLogAsync(guild, ActionType.Kick, user.Id);

            if (entry != null)
            {
                await logChannel.SendMessageAsync($"**KICK**\nExecutor: {entry.User.Mention}\nTarget: {user.Username}\nReason: {entry.Reason ?? "Unknown"}");
            }
        }

        // --- HELPER METHODS ---

        private async Task<SocketTextChannel?> GetLogChannelAsync(ulong guildId)
        {
            using (var scope = _services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BotContext>();
                var setting = await db.GuildSettings.FindAsync(guildId);
                if (setting == null || setting.LogChannelId == null) return null;

                var guild = _client.GetGuild(guildId);
                return guild?.GetTextChannel(setting.LogChannelId.Value);
            }
        }

        // --- IMPORTANT METHOD (Using Dynamic + No Fallback) ---
        private async Task<RestAuditLogEntry?> GetAuditLogAsync(SocketGuild guild, ActionType actionType, ulong targetId)
        {
            await Task.Delay(1500); // Wait for Discord to write audit log

            try
            {
                var audits = (await guild.GetAuditLogsAsync(10, actionType: actionType)
                                         .FlattenAsync())
                                         .ToList();

                foreach (var entry in audits)
                {
                    // Use dynamic to access properties without type declaration
                    // This avoids compile errors when the library is missing type definitions
                    dynamic data = entry.Data;

                    try
                    {
                        // All Action types (Ban, Kick, Move) have Target property in Data
                        if (data.Target.Id == targetId) return entry;
                    }
                    catch
                    {
                        // If Target cannot be read, skip this entry
                        continue;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}