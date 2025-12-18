using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordBotNightOwl.Data
{
    public class GuildSetting
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong GuildId { get; set; }
        public string? GuildName { get; set; }
        public ulong? AIChannelId { get; set; }
        public ulong? LogChannelId { get; set; }
    }
}