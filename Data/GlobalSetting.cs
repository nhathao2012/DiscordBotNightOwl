using System.ComponentModel.DataAnnotations;

namespace DiscordBotNightOwl.Data
{
    public class GlobalSetting
    {
        [Key]
        public int Id { get; set; }
        public ulong? HomeServerId { get; set; }
    }
}