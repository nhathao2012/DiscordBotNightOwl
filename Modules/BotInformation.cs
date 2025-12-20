using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotNightOwl.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DiscordBotNightOwl.Modules
{
    public class BotInformation : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly BotContext _db;
        private readonly IConfiguration _config;

        public BotInformation(BotContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [SlashCommand("bot-information", "Show bot information")]
        public async Task ShowStatus()
        {
            // 1. Check permission (update =>> no need)
            //if (!await IsModeratorAsync())
            //{
            //    await RespondAsync("Access Denied: This command is for Moderators only.", ephemeral: true);
            //    return;
            //}
            await DeferAsync();

            // 2. Get system info
            var client = Context.Client;
            var process = Process.GetCurrentProcess();
            var appInfo = await client.GetApplicationInfoAsync();

            // Calculate uptime
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            string uptimeString = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";

            // Calculate RAM usage in MB
            double ramUsage = process.WorkingSet64 / 1024.0 / 1024.0;

            // Get Home Server info from DB
            var globalConfig = await _db.GlobalSettings
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(); string homeServerName = "Not Set";

            if (globalConfig != null && globalConfig.HomeServerId != null)
            {
                ulong homeId = globalConfig.HomeServerId.Value;
                var homeGuild = client.GetGuild(homeId);
                homeServerName = homeGuild != null ? $"{homeGuild.Name}" : $"Unknown (ID: {homeId})";
            }

            // 3. Build embed
            var embed = new EmbedBuilder()
                .WithTitle("System Status & Diagnostics")
                .WithColor(Color.Green)
                .WithThumbnailUrl(client.CurrentUser.GetAvatarUrl() ?? client.CurrentUser.GetDefaultAvatarUrl())
                .WithCurrentTimestamp()

                // --- Bot info ---
                .AddField("Bot Identity", $"**{client.CurrentUser.Username}**#{client.CurrentUser.Discriminator}", true)
                .AddField("Creator/Owner", $"<@{appInfo.Owner.Id}>", true)
                .AddField("Home Server", homeServerName, false)

                // --- Connection info ---
                .AddField("Latency (Ping)", $"`{client.Latency} ms`", true)
                .AddField("Connection Status", $"`{client.ConnectionState}`", true)
                .AddField("Server Count", $"`{client.Guilds.Count} Servers`", true)

                // --- Hardware info ---
                .AddField("RAM Usage", $"`{ramUsage:F2} MB`", true)
                .AddField("Uptime", $"`{uptimeString}`", true)
                .AddField("Library Version", $"`Discord.Net v{DiscordConfig.Version}`", true)

                .WithFooter($"Requested by {Context.User.Username}", Context.User.GetAvatarUrl());

            await FollowupAsync(embed: embed.Build());
        }
    }
}