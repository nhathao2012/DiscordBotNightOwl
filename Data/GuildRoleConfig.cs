using System.ComponentModel.DataAnnotations;

namespace DiscordBotNightOwl.Data
{
    public class GuildRoleConfig
    {
        [Key]
        public int Id { get; set; }
        public required ulong GuildId { get; set; }
        public required ulong RoleId { get; set; }
        public string? GuildName { get; set; }
        public string? RoleName { get; set; }
        public required string ConfigType { get; set; }
    }
}