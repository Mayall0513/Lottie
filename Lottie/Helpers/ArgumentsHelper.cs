using Discord;
using Discord.WebSocket;
using Lottie.Commands.Contexts;
using Lottie.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lottie.Helpers {
    public sealed class ArgumentsHelper {
        public static ArgumentsHelper ExtractFromArguments(SocketGuildCommandContext context, params string[] arguments) {
            return new ArgumentsHelper(context.Channel, context.Guild, context.User, arguments);
        }

        public async Task<bool> AssertArgumentAsync(object argument, string[] errors) {
            if (argument != null) {
                return true;
            }

            responseBuilder.WithErrors(errors);
            await responseBuilder.SendMessageAsync();

            return false;
        }

        public bool AssertOptionalArgument(object argument) {
            return argument != null;
        }

        public async Task<bool> AssertTimeSpanRangeAsync(TimeSpan timeSpan, TimeSpan? minimum, TimeSpan? maximum) {
            if (minimum != null && timeSpan < minimum.Value) {
                responseBuilder.WithErrors($"Time span is too short, `{CommandHelper.GetResponseTimeSpan(minimum.Value)}` is the minimum");
                await responseBuilder.SendMessageAsync();

                return false;
            }

            if (maximum != null && timeSpan > maximum.Value) {
                responseBuilder.WithErrors($"Time span is too long, `{CommandHelper.GetResponseTimeSpan(maximum.Value)}` is the maximum");
                await responseBuilder.SendMessageAsync();

                return false;
            }

            return true;
        }

        public async Task<(bool presetAssertion, string presetMessage)> AssertPresetReasonAsync(Server server, PresetMessageTypes messageType, ulong messageId) {
            string message = server.GetPresetMessage(messageType, messageId);
            bool presetAssertion = message != null;

            if (!presetAssertion) {
                responseBuilder.WithErrors($"`{messageId}` is not a valid preset message ID for type `{messageType}`");
                await responseBuilder.SendMessageAsync();
            }

            return (presetAssertion, message);
        }

        private readonly IMessageChannel channel;
        private readonly SocketGuild guild;
        private readonly string[] arguments;

        private readonly ResponseBuilder responseBuilder;

        private ArgumentsHelper(IMessageChannel channel, SocketGuild guild, SocketGuildUser caller, string[] arguments) {
            this.channel = channel;
            this.guild = guild;
            this.arguments = arguments;

            responseBuilder = new ResponseBuilder(channel)
                .AsFailure()
                .WithUserSubject(caller)
                .WithText("Whoops!");
        }

        // order is always [user/users] [channel/channels] [role/roles] [time span] [preset reason] [custom reason]

        private int parsedIndex = 0;

        public ArgumentsHelper WithUser(out SocketGuildUser user, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                user = null;
                errors = new[] { "This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                user = null;
                errors = new[] { $"This command requires a user after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            string argument = arguments[parsedIndex];
            if (argument.StartsWith("<@!")) {
                argument = argument[3..^1];
            }

            else if (argument.StartsWith("<@")) {
                argument = argument[2..^1];
            }

            if (ulong.TryParse(argument, out ulong firstArgumentId)) {
                user = guild.GetUser(firstArgumentId);

                if (user == null) {
                    errors = new[] { $"Could not find user `{arguments[parsedIndex]}`" };
                }

                else {
                    errors = null;
                    parsedIndex++;
                }
            }

            else {
                user = null;
                errors = new[] { $"`{arguments[parsedIndex]}` is not a valid user" };
            }

            return this;
        }

        public ArgumentsHelper WithUsers(out SocketGuildUser[] users, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                users = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                users = null;
                errors = new[] { $"This command requires a list of users after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            List<SocketGuildUser> userList = new List<SocketGuildUser>();
            string[] withUserError;

            do {
                WithUser(out SocketGuildUser user, out withUserError);
                if (user != null) {
                    userList.Add(user);
                }
            }

            while (withUserError == null || withUserError.Length == 0);

            if (userList.Count == 0) {
                users = null;
                errors = new[] { $"This command requires a list of users after `{arguments[parsedIndex]}`" };

            }

            else {
                users = userList.ToArray();
                errors = null;
            }

            return this;
        }


        public ArgumentsHelper WithTextChannel(out SocketTextChannel channel, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                channel = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                channel = null;
                errors = new[] { $"This command requires a text channel after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            string argument = arguments[parsedIndex].Trim();
            if (argument.StartsWith("<#")) {
                argument = argument[2..^1];
            }

            if (ulong.TryParse(argument, out ulong firstArgumentId)) {
                channel = guild.GetTextChannel(firstArgumentId);

                if (channel == null) {
                    errors = new[] { $"Could not find text channel `{arguments[parsedIndex]}`" };
                }

                else {
                    errors = null;
                    parsedIndex++;
                }
            }

            else {
                argument = argument.ToLower();
                channel = guild.TextChannels.Where(channel => channel.Name.ToLower() == argument).FirstOrDefault();

                if (channel == null) {
                    errors = new[] { $"`{arguments[parsedIndex]}` is not a valid text channel" };
                }

                else {
                    errors = null;
                    parsedIndex++;
                }
            }

            return this;
        }

        public ArgumentsHelper WithTextChannels(out SocketTextChannel[] channels, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                channels = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                channels = null;
                errors = new[] { $"This command requires a list of text channels after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            List<SocketTextChannel> channelList = new List<SocketTextChannel>();
            string[] withChannelError;

            do {
                WithTextChannel(out SocketTextChannel channel, out withChannelError);
                if (channel != null) {
                    channelList.Add(channel);
                }
            }

            while (withChannelError == null || withChannelError.Length == 0);

            if (channelList.Count == 0) {
                channels = null;
                errors = new[] { $"This command requires a list of text channels after `{arguments[parsedIndex]}`" };

            }

            else {
                channels = channelList.ToArray();
                errors = null;
            }

            return this;
        }


        public ArgumentsHelper WithVoiceChannel(out SocketVoiceChannel channel, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                channel = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                channel = null;
                errors = new[] { $"This command requires a voice channel after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            string argument = arguments[parsedIndex];
            if (argument.StartsWith("<#")) {
                argument = argument[2..^1];
            }

            if (ulong.TryParse(argument, out ulong firstArgumentId)) {
                channel = guild.GetVoiceChannel(firstArgumentId);

                if (channel == null) {
                    errors = new[] { $"Could not find voice channel `{arguments[parsedIndex]}`" };
                }

                else {
                    errors = null;
                    parsedIndex++;
                }
            }

            else {
                argument = argument.ToLower();
                channel = guild.VoiceChannels.Where(channel => channel.Name.ToLower() == argument).FirstOrDefault();

                if (channel == null) {
                    errors = new[] { $"`{arguments[parsedIndex]}` is not a valid voice channel" };
                }

                else {
                    errors = null;
                    parsedIndex++;
                }
            }

            return this;
        }

        public ArgumentsHelper WithVoiceChannels(out SocketVoiceChannel[] channels, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                channels = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                channels = null;
                errors = new[] { $"This command requires a list of voice channels after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            List<SocketVoiceChannel> channelList = new List<SocketVoiceChannel>();
            string[] withChannelError;

            do {
                WithVoiceChannel(out SocketVoiceChannel channel, out withChannelError);
                if (channel != null) {
                    channelList.Add(channel);
                }
            }

            while (withChannelError == null || withChannelError.Length == 0);

            if (channelList.Count == 0) {
                channels = null;
                errors = new[] { $"This command requires a list of voice channels after `{arguments[parsedIndex]}`" };

            }

            else {
                channels = channelList.ToArray();
                errors = null;
            }

            return this;
        }


        public ArgumentsHelper WithRole(out SocketRole role, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                role = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                role = null;
                errors = new[] { $"This command requires a role after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            string argument = arguments[parsedIndex];
            if (argument.StartsWith("<@&")) {
                argument = argument[3..^1];
            }

            if (ulong.TryParse(argument, out ulong firstArgumentId)) {
                role = guild.GetRole(firstArgumentId);

                if (role == null) {
                    errors = new[] { $"Could not find role `{arguments[parsedIndex]}`" };
                }

                else {
                    errors = null;
                    parsedIndex++;
                }
            }

            else {
                argument = argument.ToLower();
                role = guild.Roles.Where(role => role.Name.ToLower() == argument).FirstOrDefault();

                if (role == null) {
                    errors = new[] { $"`{arguments[parsedIndex]}` is not a valid role" };
                }

                else {
                    errors = null;
                    parsedIndex++;
                }
            }

            return this;
        }

        public ArgumentsHelper WithRoles(out SocketRole[] roles, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                roles = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                roles = null;
                errors = new[] { $"This command requires a list of roles after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            List<SocketRole> roleList = new List<SocketRole>();
            string[] withRoleError;

            do {
                WithRole(out SocketRole channel, out withRoleError);
                if (channel != null) {
                    roleList.Add(channel);
                }
            }

            while (withRoleError == null || withRoleError.Length == 0);

            if (roleList.Count == 0) {
                roles = null;
                errors = new[] { $"This command requires a list of roles after `{arguments[parsedIndex]}`" };

            }

            else {
                roles = roleList.ToArray();
                errors = null;
            }

            return this;
        }


        public ArgumentsHelper WithTimeSpan(out TimeSpan? timeSpan, out string[] errors) {
            timeSpan = null;

            if (arguments == null || arguments.Length == 0) {
                errors = new[] { $"This command requires arguments" };
                return this;
            }

            if (arguments.Length <= parsedIndex) {
                errors = new[] { $"This command requires a time span after `{arguments[parsedIndex - 1]}`" };
                return this;
            }

            errors = null;
            int timeSpanLength = 0;

            while (parsedIndex + (timeSpanLength + 1) <= arguments.Length) {
                if (!CommandHelper.GetTimeSpan(arguments[parsedIndex..(parsedIndex + (timeSpanLength + 1))], out TimeSpan? potentialTimeSpan, out errors)) {
                    break;
                }
                
                else {
                    timeSpan = potentialTimeSpan;
                    timeSpanLength++;
                }
            }

            parsedIndex += timeSpanLength;
            return this;
        }

        public ArgumentsHelper WithPresetMessage(out ulong? messageId, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                messageId = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                messageId = null;
                errors = new[] { $"This command requires a preset reason after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            string argument = arguments[parsedIndex];
            if (argument.StartsWith("r:") && ulong.TryParse(argument[2..], out ulong potentialMessageId)) {
                messageId = potentialMessageId;
                errors = null;
                parsedIndex++;
            }

            else {
                messageId = null;
                errors = new[] { $"`{arguments[parsedIndex]}` is not a valid preset message" };
            }

            return this;
        }

        public ArgumentsHelper WithCustomReason(out string message, out string[] errors) {
            if (arguments == null || arguments.Length == 0) {
                message = null;
                errors = new[] { $"This command requires arguments" };

                return this;
            }

            if (arguments.Length <= parsedIndex) {
                message = null;
                errors = new[] { $"This command requires a reason after `{arguments[parsedIndex - 1]}`" };

                return this;
            }

            errors = null;
            message = arguments[parsedIndex++];
            
            return this;
        }
    }
}
