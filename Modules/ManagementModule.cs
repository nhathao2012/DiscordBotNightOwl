using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotNightOwl.Data;
using Microsoft.EntityFrameworkCore;

namespace DiscordBotNightOwl.Modules
{
    [RequireContext(ContextType.Guild)]
    public class ManagementModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly BotContext _db;
        private readonly IConfiguration _config;

        public ManagementModule(BotContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // --- UNIFIED PERMISSION CHECK ---
        // Access granted if User is: Server Owner OR Discord Administrator OR Has Bot Moderator Role
        private async Task<bool> IsManagerAsync()
        {
            var user = Context.User as SocketGuildUser;
            if (user == null) return false;

            // 1. Check Hard Permissions (Server Owner or Discord Admin)
            if (user.Id == Context.Guild.OwnerId || user.GuildPermissions.Administrator) return true;

            // 2. Check Database for Moderator Role
            string modType = _config["RoleTypes:Mod"] ?? "MOD_ACCESS";

            var modRoleIds = await _db.GuildRoleConfigs
                .Where(x => x.GuildId == Context.Guild.Id && x.ConfigType == modType)
                .Select(x => x.RoleId)
                .ToListAsync();

            return user.Roles.Any(r => modRoleIds.Contains(r.Id));
        }

        // =========================================================================
        // SECTION 1: CONFIGURATION COMMANDS
        // =========================================================================

        [SlashCommand("set_ai_channel", "Select channel for bot AI conversations")]
        public async Task SetAiChannel(SocketChannel channel)
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            var setting = await _db.GuildSettings.FindAsync(Context.Guild.Id);
            string currentGuildName = Context.Guild.Name;

            if (setting == null)
            {
                setting = new GuildSetting
                {
                    GuildId = Context.Guild.Id,
                    GuildName = currentGuildName,
                    AIChannelId = channel.Id
                };
                _db.GuildSettings.Add(setting);
            }
            else
            {
                setting.AIChannelId = channel.Id;
                setting.GuildName = currentGuildName;
            }

            await _db.SaveChangesAsync();
            await RespondAsync($"Configuration Saved. AI Chat Channel: <#{channel.Id}>.");
        }

        [SlashCommand("set_log", "Set log channel for system events")]
        public async Task SetLogChannel(SocketTextChannel channel)
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            var setting = await _db.GuildSettings.FindAsync(Context.Guild.Id);
            string currentGuildName = Context.Guild.Name;

            if (setting == null)
            {
                setting = new GuildSetting
                {
                    GuildId = Context.Guild.Id,
                    GuildName = currentGuildName,
                    LogChannelId = channel.Id
                };
                _db.GuildSettings.Add(setting);
            }
            else
            {
                setting.LogChannelId = channel.Id;
                setting.GuildName = currentGuildName;
            }

            await _db.SaveChangesAsync();
            await RespondAsync($"Configuration Saved. System logs will be sent to {channel.Mention}.");
        }

        [SlashCommand("add_ai_role", "Authorize a role to use AI chat")]
        public async Task AddAiRole(SocketRole role)
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            string aiType = _config["RoleTypes:AI"] ?? "AI_ACCESS";

            bool exists = await _db.GuildRoleConfigs.AnyAsync(x =>
                x.GuildId == Context.Guild.Id && x.RoleId == role.Id && x.ConfigType == aiType);

            if (exists)
            {
                await RespondAsync("This role is already authorized.", ephemeral: true);
                return;
            }

            _db.GuildRoleConfigs.Add(new GuildRoleConfig
            {
                GuildId = Context.Guild.Id,
                RoleId = role.Id,
                GuildName = Context.Guild.Name,
                RoleName = role.Name,
                ConfigType = aiType
            });
            await _db.SaveChangesAsync();
            await RespondAsync($"Role **{role.Name}** is now authorized to use AI.");
        }

        [SlashCommand("add_mod_role", "Authorize a role to manage the bot")]
        public async Task AddModRole(SocketRole role)
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            string modType = _config["RoleTypes:Mod"] ?? "MOD_ACCESS";

            bool exists = await _db.GuildRoleConfigs.AnyAsync(x =>
                x.GuildId == Context.Guild.Id && x.RoleId == role.Id && x.ConfigType == modType);

            if (exists)
            {
                await RespondAsync("This role is already a Manager.", ephemeral: true);
                return;
            }

            _db.GuildRoleConfigs.Add(new GuildRoleConfig
            {
                GuildId = Context.Guild.Id,
                RoleId = role.Id,
                GuildName = Context.Guild.Name,
                RoleName = role.Name,
                ConfigType = modType
            });
            await _db.SaveChangesAsync();
            await RespondAsync($"Role **{role.Name}** is now a Manager (Full Access).");
        }

        // =========================================================================
        // SECTION 2: ENFORCEMENT COMMANDS
        // =========================================================================

        [SlashCommand("kick", "Kick a member from the server")]
        public async Task KickCmd(SocketGuildUser user, string reason = "No reason provided")
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            try
            {
                await user.KickAsync(reason);
                await RespondAsync($"User **{user.Username}** was KICKED. Reason: {reason}");
            }
            catch
            {
                await RespondAsync("Error: Bot hierarchy is lower than the target user.", ephemeral: true);
            }
        }

        [SlashCommand("ban", "Ban a member permanently")]
        public async Task BanCmd(SocketGuildUser user, string reason = "Violation of rules")
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            try
            {
                await Context.Guild.AddBanAsync(user, 0, reason);
                await RespondAsync($"User **{user.Username}** was BANNED. Reason: {reason}");
            }
            catch
            {
                await RespondAsync("Error: Bot hierarchy is lower than the target user.", ephemeral: true);
            }
        }

        [SlashCommand("timeout", "Restrict user chat/voice access")]
        public async Task TimeoutCmd(SocketGuildUser user, int minutes)
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            try
            {
                await user.SetTimeOutAsync(TimeSpan.FromMinutes(minutes));
                await RespondAsync($"User **{user.Username}** timed out for {minutes} minutes.");
            }
            catch
            {
                await RespondAsync("Error: Bot hierarchy is lower than the target user.", ephemeral: true);
            }
        }

        [SlashCommand("grant_role", "Assign a role to a user")]
        public async Task GiveRoleCmd(SocketGuildUser user, SocketRole role)
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied: You are not a Manager.", ephemeral: true);
                return;
            }

            try
            {
                await user.AddRoleAsync(role);
                await RespondAsync($"Granted role **{role.Name}** to **{user.Username}**.");
            }
            catch
            {
                await RespondAsync("Error: Bot role must be higher than the target role.", ephemeral: true);
            }
        }

        [SlashCommand("help", "Show all management commands")]
        public async Task HelpCmd()
        {
            if (!await IsManagerAsync())
            {
                await RespondAsync("Permission Denied.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Management Interface")
                .WithColor(Color.DarkBlue)
                .WithDescription("Full access controls for Managers.")
                .AddField("Configuration", "`/set_log`, `/set_ai_channel`, `/add_mod_role`, `/add_ai_role`")
                .AddField("Enforcement", "`/kick`, `/ban`, `/timeout`, `/grant_role`")
                .Build();

            await RespondAsync(embed: embed, ephemeral: true);
        }
    }
}