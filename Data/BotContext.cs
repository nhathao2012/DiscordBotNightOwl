using Microsoft.EntityFrameworkCore;

namespace DiscordBotNightOwl.Data
{
    public class BotContext : DbContext
    {
        public DbSet<VoiceSession> VoiceSessions { get; set; }
        public DbSet<GuildRoleConfig> GuildRoleConfigs { get; set; }
        public DbSet<GuildSetting> GuildSettings { get; set; }
        public DbSet<GlobalSetting> GlobalSettings { get; set; }

        public BotContext(DbContextOptions<BotContext> options) : base(options)
        {

        }

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //{
        //    string currentPath = Directory.GetCurrentDirectory();
        //    string dbFolder = Path.Combine(currentPath, "Database");
        //    if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

        //    string dbPath = Path.Combine(dbFolder, "BotData.db");
        //    options.UseSqlite($"Data Source={dbPath}");
        //}
    }
}