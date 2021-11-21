using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;

namespace Lottie.Commands.Contexts {
    public sealed class SocketGuildCommandContext : ICommandContext {
        public DiscordSocketClient Client { get; }
        public SocketGuild Guild { get; }
        public ISocketMessageChannel Channel { get; }
        public SocketGuildUser User { get; }
        public SocketUserMessage Message { get; }
        public DateTime CommandTime { get; }

        public SocketGuildCommandContext(DiscordSocketClient client, SocketUserMessage message) {
            Client = client;
            Guild = (message.Channel as SocketGuildChannel)?.Guild;
            Channel = message.Channel;
            User = message.Author as SocketGuildUser;
            Message = message;
            CommandTime = DateTime.UtcNow;
        }

        IDiscordClient ICommandContext.Client => Client;
        IGuild ICommandContext.Guild => Guild;
        IMessageChannel ICommandContext.Channel => Channel;
        IUser ICommandContext.User => User;
        IUserMessage ICommandContext.Message => Message;
    }
}
