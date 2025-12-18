using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotNightOwl.Services;
using System.Reflection;

namespace DiscordBotNightOwl
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly VoiceStateService _voiceService;
        private readonly ChatListenerService _chatListener;
        private readonly LoggingService _loggingService;

        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _serviceProvider;

        public Worker(ILogger<Worker> logger,
                      DiscordSocketClient client,
                      IConfiguration configuration,
                      VoiceStateService voiceService,
                      InteractionService interactionService,
                      IServiceProvider serviceProvider,
                      ChatListenerService chatListener,
                      LoggingService loggingService)
        {
            _logger = logger;
            _client = client;
            _configuration = configuration;
            _voiceService = voiceService;
            _interactionService = interactionService;
            _serviceProvider = serviceProvider;
            _chatListener = chatListener;
            _loggingService = loggingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _client.Log += LogAsync;
            _interactionService.Log += LogAsync;

            _client.Ready += ReadyAsync;
            _client.UserVoiceStateUpdated += _voiceService.OnVoiceStateUpdated;

            _client.MessageReceived += _chatListener.OnMessageReceived;
            _client.InteractionCreated += async (interaction) =>
            {
                var ctx = new SocketInteractionContext(_client, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
            };

            _client.UserVoiceStateUpdated += _loggingService.OnVoiceStateUpdated;
            _client.GuildMemberUpdated += _loggingService.OnGuildMemberUpdated;
            _client.UserBanned += _loggingService.OnUserBanned;
            _client.UserLeft += _loggingService.OnUserLeft;

            var token = _configuration["Discord:Token"];
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1, stoppingToken);
        }

        private Task LogAsync(LogMessage log)
        {
            _logger.LogInformation(log.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            try
            {
                // 1. Load all command modules from code
                await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

                // 2. REGISTER GLOBALLY (production mode)
                await _interactionService.RegisterCommandsGloballyAsync(true);

                // 3. FIX DUPLICATE COMMANDS (cleanup old entries)
                foreach (var guild in _client.Guilds)
                {
                    // Send empty list to remove guild-specific commands
                    await guild.DeleteApplicationCommandsAsync();
                }

                _logger.LogInformation($"--- BOT ONLINE: {_client.CurrentUser} ---");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error when adding commands!");
            }
        }
    }
}