using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotNightOwl;
using DiscordBotNightOwl.Data;
using DiscordBotNightOwl.Services;
using Microsoft.EntityFrameworkCore;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = Host.CreateApplicationBuilder(args);

// 1. Discord Configuration
//var socketConfig = new DiscordSocketConfig
//{
//    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
//    UseInteractionSnowflakeDate = false
//};
var socketConfig = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds
                   | GatewayIntents.GuildMessages
                   | GatewayIntents.MessageContent
                   | GatewayIntents.GuildVoiceStates
                   | GatewayIntents.GuildMembers,
    UseInteractionSnowflakeDate = false
};
builder.Services.AddSingleton(socketConfig);
builder.Services.AddSingleton<DiscordSocketClient>();

// 2. Slash Command Configuration
builder.Services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()));

// 3. Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BotContext>(options =>
    options.UseNpgsql(connectionString));

// 4. Register Services 
builder.Services.AddSingleton<VoiceStateService>();
builder.Services.AddSingleton<ChatListenerService>();

// 5. Logging Service Configuration
builder.Services.AddSingleton<LoggingService>();

// 6. AI Service Configuration
builder.Services.AddHttpClient<AIService>();

// 7. Worker Configuration
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BotContext>();
        context.Database.Migrate();
        Console.WriteLine("--> Connected to Cloud Database & Migrated successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"--> Database Error: {ex.Message}");
    }
}

host.Run();