using Discord;
using Discord.Interactions;
using DiscordBotNightOwl.Data;
using Microsoft.EntityFrameworkCore;
using System;

namespace DiscordBotNightOwl.Modules
{
    public class OwnerModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly BotContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public OwnerModule(BotContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // Verify owner permission by checking Discord API
        private async Task<bool> IsOwnerAsync()
        {
            var appInfo = await Context.Client.GetApplicationInfoAsync();
            return Context.User.Id == appInfo.Owner.Id;
        }

        // --- COMMAND 1: SET HOME SERVER ---
        [SlashCommand("set_home", "Set this server as Home Server (Owner Only)")]
        public async Task SetHomeServer()
        {
            if (!await IsOwnerAsync())
            {
                await RespondAsync("You are not the BOT Owner!", ephemeral: true);
                return;
            }

            var global = await _db.GlobalSettings.FirstOrDefaultAsync();

            if (global == null)
            {
                global = new GlobalSetting { HomeServerId = Context.Guild.Id };
                _db.GlobalSettings.Add(global);
            }
            else
            {
                global.HomeServerId = Context.Guild.Id;
            }

            await _db.SaveChangesAsync();
            await RespondAsync($"**Config Saved!** Home Server is now: **{Context.Guild.Name}**");
        }

        // --- COMMAND 2: MANUAL UPDATE AVATAR ---
        [SlashCommand("update_avatar", "Force update Bot's avatar from Home Server icon")]
        public async Task ForceUpdateAvatar()
        {
            if (!await IsOwnerAsync())
            {
                await RespondAsync("You are not the BOT Owner!", ephemeral: true);
                return;
            }

            await DeferAsync();

            try
            {
                var global = await _db.GlobalSettings.FirstOrDefaultAsync();

                if (global == null || global.HomeServerId == null)
                {
                    await FollowupAsync("Error: Home Server is not set. Use `/set_home` first.");
                    return;
                }

                ulong guildId = global.HomeServerId.Value;

                var homeGuild = Context.Client.GetGuild(guildId);
                if (homeGuild == null)
                {
                    await FollowupAsync("Error: Bot is not in the Home Server anymore.");
                    return;
                }

                string iconUrl = homeGuild.IconUrl;
                if (string.IsNullOrEmpty(iconUrl))
                {
                    await FollowupAsync($"The server **{homeGuild.Name}** has no icon.");
                    return;
                }

                using (var httpClient = _httpClientFactory.CreateClient())
                {
                    using (var stream = await httpClient.GetStreamAsync(iconUrl))
                    {
                        var image = new Image(stream);
                        await Context.Client.CurrentUser.ModifyAsync(u => u.Avatar = image);
                    }
                }

                await FollowupAsync($"**Success!** Bot avatar updated to match **{homeGuild.Name}**.");
            }
            catch (Exception ex)
            {
                await FollowupAsync($"**Failed:** {ex.Message}");
            }
        }

        // --- COMMAND 3: NUKE DATABASE (CAREFUL WITH THIS SECTION) ---
        [SlashCommand("nuke_database", "PERMANENTLY DELETE DATABASE")]
        public async Task NukeDatabase([Summary("password", "Enter confirmation password")] string confirmCode)
        {
            // Verify owner permission (strictly restricted)
            if (!await IsOwnerAsync())
            {
                await RespondAsync("**ACCESS DENIED**", ephemeral: true);
                return;
            }

            // Verify confirmation code (prevent accidental execution)
            string? secretKey = _config["Owner:NukeDBCode"];
            if (confirmCode != secretKey)
            {
                await RespondAsync("Authentication Failed: Invalid password provided.", ephemeral: true);
                return;
            }

            await DeferAsync();

            try
            {
                var nukeSql = @"DROP SCHEMA public CASCADE;
                              CREATE SCHEMA public;
                              GRANT ALL ON SCHEMA public TO postgres;
                              GRANT ALL ON SCHEMA public TO public;";

                await _db.Database.ExecuteSqlRawAsync(nukeSql);

                await _db.Database.MigrateAsync();

                await FollowupAsync("Database has been NUKED and Re-built successfully!");
            }
            catch (Exception)
            {
                await FollowupAsync("DB is opened in another program");
            }
        }
        [SlashCommand("shutdown", "Terminate the bot process (Owner only)")]
        public async Task ShutdownBot(
            [Summary("password", "Authorization code required to shut down the system")] string password)
        {
            // 1. Permission Check
            if (!await IsOwnerAsync())
            {
                await RespondAsync("Access Denied: You do not have permission to execute this command.", ephemeral: true);
                return;
            }

            // 2. Retrieve Configuration
            string? secretPass = _config["Owner:ShutdownCode"];

            // 3. Validate Password
            if (password != secretPass)
            {
                await RespondAsync("Authentication Failed: Invalid password provided.", ephemeral: true);
                return;
            }

            // 4. Execution
            await RespondAsync("I'm turning off, goodbye.");

            // Run in background task to prevent blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    // Gracefully disconnect the bot
                    await Context.Client.SetStatusAsync(UserStatus.Offline);
                    await Context.Client.StopAsync();
                    await Context.Client.LogoutAsync();

                    // Wait briefly to ensure disconnection
                    await Task.Delay(1000);
                }
                catch
                {
                    // Ignore exceptions during shutdown
                }
                finally
                {
                    // Forcefully terminate the process
                    Environment.Exit(0);
                }
            });
        }
    }
}