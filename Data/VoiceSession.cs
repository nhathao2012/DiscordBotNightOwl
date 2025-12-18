using System.ComponentModel.DataAnnotations;

namespace DiscordBotNightOwl.Data
{
    public class VoiceSession
    {
        [Key]
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public string? GuildName { get; set; }
        public string? ChannelName { get; set; }
        public string? UserName { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public double DurationMinutes { get; set; }
    }
}
