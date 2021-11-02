using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace DiscordBot6.Commands.Contexts {
    public sealed class SocketGuildCommandContext : ICommandContext {
        public DiscordSocketClient Client { get; }
        public SocketGuild Guild { get; }
        public ISocketMessageChannel Channel { get; }
        public SocketGuildUser User { get; }
        public SocketUserMessage Message { get; }

        public SocketGuildCommandContext(DiscordSocketClient client, SocketUserMessage message) {
            Client = client;
            Guild = (message.Channel as SocketGuildChannel)?.Guild;
            Channel = message.Channel;
            User = message.Author as SocketGuildUser;
            Message = message;
        }

        IDiscordClient ICommandContext.Client => Client;
        IGuild ICommandContext.Guild => Guild;
        IMessageChannel ICommandContext.Channel => Channel;
        IUser ICommandContext.User => User;
        IUserMessage ICommandContext.Message => Message;
    }
}
