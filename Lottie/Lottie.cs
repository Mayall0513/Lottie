using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lottie.Commands;
using Lottie.Commands.Contexts;
using Lottie.Database;
using Lottie.Helpers;
using Lottie.Timing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Lottie {
    public static class Lottie {
        public static DiscordShardedClient Client { get; private set; }
        private static CommandService commandService;

        public static readonly IEmote NextPageEmoji = new Emoji("➡️");
        public static readonly IEmote PreviousPageEmoji = new Emoji("⬅️");
        public static readonly IEmote RefreshPageEmoji = new Emoji("🔄");

        public const int PaginationSize = 8;

        public const string DiscordNewLine = "\n";
        public const string DefaultCommandPrefix = "+";

        public static async Task Main(string[] _0) {
            DiscordSocketConfig discordSocketConfig = new DiscordSocketConfig {
                MessageCacheSize = 40,
                GatewayIntents = GatewayIntents.All
            };

            Client = new DiscordShardedClient(discordSocketConfig);
            Client.ShardReady += Client_ShardReady;
#if DEBUG
            Client.Log += Client_Log;
#endif
            commandService = new CommandService();
            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            await Client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["BotToken"], true);
            await Client.StartAsync();
            await Task.Delay(-1);
        }
#if DEBUG
        private static Task Client_Log(LogMessage logMessage) {
            Console.WriteLine(logMessage.ToString());
            return Task.CompletedTask;
        }
#endif
        private static async Task Client_ShardReady(DiscordSocketClient client) {
            Server server = null;

            await foreach (MutePersist mutePersist in Repository.GetMutePersistsAllAsync(client.Guilds.Select(guild => guild.Id))) {
                if (server == null || server.Id != mutePersist.ServerId) {
                    server = await Repository.GetServerAsync(mutePersist.ServerId);
                }

                User user = await server.GetUserAsync(mutePersist.UserId);
                if (mutePersist.Expired) {
                    await Repository.RemoveMutePersistedAsync(mutePersist.ServerId, mutePersist.UserId, mutePersist.ChannelId);
                    continue;
                }

                user.PrecacheMutePersisted(mutePersist);
            }

            await foreach (RolePersist rolePersist in Repository.GetRolePersistsAllAsync(client.Guilds.Select(guild => guild.Id))) {
                if (server == null || server.Id != rolePersist.ServerId) {
                    server = await Repository.GetServerAsync(rolePersist.ServerId);
                }

                User user = await server.GetUserAsync(rolePersist.UserId);
                if (rolePersist.Expired) {
                    await Repository.RemoveRolePersistedAsync(rolePersist.ServerId, rolePersist.UserId, rolePersist.RoleId);
                    continue;
                }

                user.PrecacheRolePersisted(rolePersist);
            }

            // Assign events
            client.MessageReceived += Client_MessageReceived;
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;
            client.ButtonExecuted += Client_ButtonExecuted;
            client.UserJoined += Client_UserJoined;
        }

        private static async Task Client_MessageReceived(SocketMessage socketMessage) {
            if (!(socketMessage is SocketUserMessage socketUserMessage)) {
                return;
            }

            if (socketMessage.Channel is SocketGuildChannel socketGuildChannel) { // message was sent in a server
                Server server = await socketGuildChannel.Guild.L_GetServerAsync();

                if (socketMessage.Author.Id != socketGuildChannel.Guild.CurrentUser.Id && server.IsCommandChannel(socketMessage.Channel.Id)) { // message was not sent by the bot and was sent in a command channel
                    int argumentIndex = 0;

                    if (socketUserMessage.HasStringPrefix(server.GetCommandPrefix(), ref argumentIndex)) {
                        SocketGuildCommandContext commandContext = new SocketGuildCommandContext(Client.GetShardFor(socketGuildChannel.Guild), socketUserMessage);
                        await commandService.ExecuteAsync(commandContext, argumentIndex, null);
                    }
                }

                /*
                // this is unfinished - i want to do a thing where the user can decide what happens
                // just a placeholder for the moment!

                IEnumerable<PhraseRule> phraseRules = await server.GetPhraseRuleSetsAsync();

                foreach (PhraseRule phraseRule in phraseRules) {
                    if (phraseRule.CanApply(socketMessage) && phraseRule.Matches(socketMessage.Content)) {
                        await socketUserMessage.DeleteAsync();
                        break;
                    }
                }
                */
            }
        }

        private static async Task Client_UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState beforeState, SocketVoiceState afterState) {
            if (afterState.VoiceChannel == null) { // they just left
                return; // there's nothing we want to do if a user leaves the channel
            }

            if (socketUser is SocketGuildUser socketGuildUser) { // this happened in a server's voice channel
                Extensions.CompareVoiceStatuses(beforeState, afterState, out VoiceStatusUpdate voiceStatusUpdate);

                Server server = await socketGuildUser.Guild.L_GetServerAsync();
                User user = await socketGuildUser.L_GetUserAsync();

                if (user.CheckVoiceStatusUpdate(voiceStatusUpdate)) {
                    return;
                }

                if (beforeState.VoiceChannel == null) { // they just joined
                    if (user.GlobalMutePersisted && !afterState.IsMuted) { // user is not muted and should be 
                        user.AddVoiceStatusUpdate(1, 0);
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }

                    if (user.GlobalDeafenPersisted && !afterState.IsDeafened) { // user is not deafened and should be
                        user.AddVoiceStatusUpdate(0, 1);
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Deaf = true; });
                    }
                }

                else { // something else happened
                    bool muteChanged = beforeState.IsMuted != afterState.IsMuted;
                    bool deafenChanged = beforeState.IsDeafened != afterState.IsDeafened;

                    if (muteChanged) {
                        if (!afterState.IsMuted) {
                            await user.RemoveMutePersistedAsync(afterState.VoiceChannel.Id);
                        }  

                        if (server.AutoMutePersist) {
                            user.GlobalMutePersisted = afterState.IsMuted;
                        }
                    }

                    if (server.AutoDeafenPersist && deafenChanged) { // the user was (un)muted or (un)deafened AND the server wants to automatically persist the change
                        user.GlobalDeafenPersisted = afterState.IsDeafened;
                    }

                    await server.SetUserAsync(user);
                }

                if (afterState.VoiceChannel != null && (beforeState.VoiceChannel != afterState.VoiceChannel) && !user.GlobalMutePersisted) { // the user moved channels AND they are not globally mute persisted
                    IEnumerable<ulong> mutePersists = user.GetMutesPersistedIds(); // get channel specific mute persists
                    bool channelPersisted = mutePersists.Contains(afterState.VoiceChannel.Id);

                    if (channelPersisted != afterState.IsMuted) { // something needs to be changed
                        user.AddVoiceStatusUpdate(channelPersisted ? 1 : -1, 0);
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = channelPersisted; });
                    }
                }
            }
        }

        private static async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> beforeUser, SocketGuildUser afterUser) {
            Extensions.CompareMemberStatuses(beforeUser.Value, afterUser, out MemberStatusUpdate memberStatusUpdate);
            if (memberStatusUpdate.RolesAdded.Count == 0 && memberStatusUpdate.RolesRemoved.Count == 0) { // no roles were changed
                return;
            }

            Server server = await beforeUser.Value.Guild.L_GetServerAsync();
            User user = await beforeUser.Value.L_GetUserAsync();
            if (user.CheckMemberStatusUpdate(memberStatusUpdate)) {
                return;
            }

            if (server.AutoRolePersist && memberStatusUpdate.RolesAdded.Count > 0) {
                await user.AddRolesPersistedAsync(memberStatusUpdate.RolesAdded, null);
            }

            if (memberStatusUpdate.RolesRemoved.Count > 0) {
                await user.RemoveRolesPersistedAsync(memberStatusUpdate.RolesRemoved);
            }

            await user.ApplyContingentRolesAsync(afterUser, beforeUser.Value.GetRoleIds(), afterUser.GetRoleIds());
        }

        private static async Task Client_ButtonExecuted(SocketMessageComponent messageComponent) {
            if (!(messageComponent.User is SocketGuildUser socketGuildUser)) {
                await messageComponent.DeferAsync();
                return;
            }

            Server server = await socketGuildUser.Guild.L_GetServerAsync();
            User user = await socketGuildUser.L_GetUserAsync();

            int pageIndex = messageComponent.Data.CustomId.IndexOf('@');
            int userIndex = messageComponent.Data.CustomId.IndexOf('|', pageIndex);
            int infoIndex = messageComponent.Data.CustomId.IndexOf('&', userIndex);

            int.TryParse(messageComponent.Data.CustomId[(pageIndex + 1)..userIndex], out int page);
            ulong.TryParse(messageComponent.Data.CustomId[(userIndex + 1)..infoIndex], out ulong callerId);

            if (socketGuildUser.Id != callerId) {
                await messageComponent.DeferAsync();
                return;
            }

            ResponseBuilder responseBuilder = null;

            if (messageComponent.Data.CustomId.StartsWith("rolepersist")) {
                if (!await server.UserMatchesConstraintsAsync(ConstraintIntents.ROLEPERSIST_CHECK, null, socketGuildUser.GetRoleIds(), socketGuildUser.Id)) {
                    await messageComponent.DeferAsync();
                    return;
                }

                if (messageComponent.Data.CustomId.StartsWith("rolepersist_list")) {
                    ulong.TryParse(messageComponent.Data.CustomId[(infoIndex + 1)..], out ulong roleId);
                    SocketRole role = socketGuildUser.Guild.GetRole(roleId);

                    if (role != null) {
                        responseBuilder = RolePersists_ListUsers.CreateResponse(socketGuildUser.Guild, server, socketGuildUser, role, page, null);
                    }
                }

                else {
                    ulong.TryParse(messageComponent.Data.CustomId[(infoIndex + 1)..], out ulong calleeId);
                    SocketGuildUser callee = socketGuildUser.Guild.GetUser(calleeId);

                    if (callee != null) {
                        responseBuilder = await RolePersists_CheckUser.CreateResponseAsync(socketGuildUser.Guild, socketGuildUser, callee, page, null);
                    }
                }
            }

            if (messageComponent.Data.CustomId.StartsWith("channelmutepersist")) {
                if (!await server.UserMatchesConstraintsAsync(ConstraintIntents.CHANNELMUTE_CHECK, null, socketGuildUser.GetRoleIds(), socketGuildUser.Id)) {
                    await messageComponent.DeferAsync();
                    return;
                }

                if (messageComponent.Data.CustomId.StartsWith("channelmutepersist_list")) {
                    ulong.TryParse(messageComponent.Data.CustomId[(infoIndex + 1)..], out ulong channelId);
                    SocketVoiceChannel voiceChannel = socketGuildUser.Guild.GetVoiceChannel(channelId);

                    if (voiceChannel != null) {
                        responseBuilder = ChannelMutePersists_ListUsers.CreateResponse(socketGuildUser.Guild, server, socketGuildUser, voiceChannel, page, null);
                    }
                }

                else {
                    ulong.TryParse(messageComponent.Data.CustomId[(infoIndex + 1)..], out ulong calleeId);
                    SocketGuildUser callee = socketGuildUser.Guild.GetUser(calleeId);

                    if (callee != null) {
                        responseBuilder = await ChannelMutePersists_CheckUser.CreateResponseAsync(socketGuildUser.Guild, socketGuildUser, callee, page, null);
                    }
                }
            }

            if (responseBuilder != null) {
                await messageComponent.UpdateAsync(messageProperties => {
                    messageProperties.Embed = responseBuilder.GetEmbed();
                    messageProperties.Components = responseBuilder.GetMessageComponent();
                });
            }

            else {
                await messageComponent.DeferAsync();
            }
        }

        private static async Task Client_UserJoined(SocketGuildUser socketUser) {
            User user = await socketUser.L_GetUserAsync();
            IEnumerable<ulong> rolesPersisted = user.GetRolesPersistedIds();

            if (rolesPersisted.Any()) { // this user has role persists on this server
                IEnumerable<ulong> persistlessContingentRoles = (await user.GetActiveContingentRoleIdsAsync()).Except(rolesPersisted);
                foreach(ulong contingentRole in persistlessContingentRoles) {
                    await user.RemoveActiveContingentRoleAsync(contingentRole);
                }

                IEnumerable<ulong> userRoleIds = socketUser.Guild.Roles.Where(role => role.Position < socketUser.Guild.CurrentUser.Hierarchy)
                    .Select(role => role.Id).Intersect(rolesPersisted)
                    .Except(await user.GetContingentRolesRemovedAsync());

                if (userRoleIds.Any()) {
                    await socketUser.AddRolesAsync(userRoleIds);
                }
            }
        }
    }
}
