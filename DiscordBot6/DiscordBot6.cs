using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.Commands.Contexts;
using DiscordBot6.Database;
using DiscordBot6.Helpers;
using DiscordBot6.Timing;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public static class DiscordBot6 {
        public static DiscordShardedClient Client { get; private set; }
        public static CommandService CommandService { get; private set; }

        public const string DiscordNewLine = "\n";
        public const string DefaultCommandPrefix = "+";

        public static async Task Main(string[] _0) {
            Client = new DiscordShardedClient();

            CommandService = new CommandService();
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            Client.ShardReady += Client_ShardReady;
            Client.MessageReceived += Client_MessageReceived;
            Client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
            Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            Client.UserJoined += Client_UserJoined;

            await Client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["BotToken"], true);
            await Client.StartAsync();
            await Task.Delay(-1);
        }

        private static async Task Client_ShardReady(DiscordSocketClient client) {
            await foreach (MutePersist mutePersist in Repository.GetMutePersistsAllAsync(client.Guilds.Select(guild => guild.Id))) {
                Server server = await Server.GetServerAsync(mutePersist.ServerId);
                User user = await server.GetUserAsync(mutePersist.UserId);

                if (mutePersist.Expired) {
                    await Repository.RemoveMutePersistedAsync(mutePersist.ServerId, mutePersist.UserId, mutePersist.ChannelId);
                    continue;
                }

                user.PrecacheMutePersisted(mutePersist);
            }

            await foreach (RolePersist rolePersist in Repository.GetRolePersistsAllAsync(client.Guilds.Select(guild => guild.Id))) {
                Server server = await Server.GetServerAsync(rolePersist.ServerId);
                User user = await server.GetUserAsync(rolePersist.UserId);

                if (rolePersist.Expired) {
                    await Repository.RemoveRolePersistedAsync(rolePersist.ServerId, rolePersist.UserId, rolePersist.RoleId);
                    continue;
                }

                user.PrecacheRolePersisted(rolePersist);
            }
        }

        private static async Task Client_MessageReceived(SocketMessage socketMessage) {
            if (!(socketMessage is SocketUserMessage socketUserMessage)) {
                return;
            }

            if (socketMessage.Channel is SocketGuildChannel socketGuildChannel) { // message was sent in a server
                Server server = await socketGuildChannel.Guild.GetServerAsync();

                if (socketMessage.Author.Id != socketGuildChannel.Guild.CurrentUser.Id && server.IsCommandChannel(socketMessage.Channel.Id)) { // message was not sent by the bot and was sent in a command channel
                    int argumentIndex = 0;

                    if (socketUserMessage.HasStringPrefix(server.GetCommandPrefix(), ref argumentIndex)) {
                        SocketGuildCommandContext commandContext = new SocketGuildCommandContext(Client.GetShardFor(socketGuildChannel.Guild), socketUserMessage);
                        await CommandService.ExecuteAsync(commandContext, argumentIndex, null);
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

        private static async Task Client_UserVoiceStateUpdated (SocketUser socketUser, SocketVoiceState beforeVoiceState, SocketVoiceState afterVoiceState) {
            if (afterVoiceState.VoiceChannel == null) { // they just left
                return; // there's nothing we want to do if a user leaves the channel
            }

            if (socketUser is SocketGuildUser socketGuildUser) { // this happened in a server's voice channel
                Server server = await socketGuildUser.Guild.GetServerAsync();
                User user = await server.GetUserAsync(socketUser.Id);

                bool botTriggered = user.DecrementRolesUpdated();

                if (beforeVoiceState.VoiceChannel == null) { // they just joined
                    if (user.GlobalMutePersisted && !afterVoiceState.IsMuted) { // user is not muted and should be 
                        user.IncrementVoiceStatusUpdated();
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }

                    if (user.GlobalDeafenPersisted && !afterVoiceState.IsDeafened) { // user is not deafened and should be
                        user.IncrementVoiceStatusUpdated();
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Deaf = true; });
                    }
                }

                else { // something else happened
                    bool muteChanged = beforeVoiceState.IsMuted != afterVoiceState.IsMuted;
                    bool deafenChanged = beforeVoiceState.IsDeafened != afterVoiceState.IsDeafened;

                    if (muteChanged) {
                        if (!afterVoiceState.IsMuted) {
                            await user.RemoveMutePersistedAsync(afterVoiceState.VoiceChannel.Id);
                        }  

                        if (!botTriggered && server.AutoMutePersist) {
                            user.GlobalMutePersisted = afterVoiceState.IsMuted;
                        }
                    }

                    if (!botTriggered && server.AutoDeafenPersist && deafenChanged) { // the user was (un)muted or (un)deafened AND the server wants to automatically persist the change
                        user.GlobalDeafenPersisted = afterVoiceState.IsDeafened;
                    }

                    await server.SetUserAsync(socketGuildUser.Id, user);
                }

                if (afterVoiceState.VoiceChannel != null && (beforeVoiceState.VoiceChannel != afterVoiceState.VoiceChannel) && !user.GlobalMutePersisted) { // the user moved channels AND they are not globally mute persisted
                    IEnumerable<ulong> mutePersists = user.GetMutesPersistedIds(); // get channel specific mute persists
                    bool channelPersisted = mutePersists.Contains(afterVoiceState.VoiceChannel.Id);

                    if (channelPersisted != afterVoiceState.IsMuted) { // something needs to be changed
                        user.IncrementVoiceStatusUpdated();
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = channelPersisted; });
                    }
                }
            }
        }

        private static async Task Client_GuildMemberUpdated(SocketGuildUser beforeUser, SocketGuildUser afterUser) {
            // we're only interested in the IDs of the roles added and removed (and after too for checking for contingent roles)
            HashSet<ulong> rolesAfter = new HashSet<ulong>(afterUser.Roles.Select(role => role.Id));

            HashSet<ulong> rolesAdded = new HashSet<ulong>(rolesAfter);
            HashSet<ulong> rolesRemoved = new HashSet<ulong>(beforeUser.Roles.Select(role => role.Id));

            rolesAdded.ExceptWith(rolesRemoved); // remove all of the roles the user had at the start from those they had at the end to get a list of additions
            rolesRemoved.ExceptWith(rolesAfter); // remove all of the roles the user had at the end from those they had at the start to get a list of removals

            if (rolesAdded.Count == 0 && rolesRemoved.Count == 0) { // no roles were changed
                return;
            }

            Server server = await beforeUser.Guild.GetServerAsync();
            User user = await server.GetUserAsync(beforeUser.Id);

            bool botTriggered = user.DecrementRolesUpdated();

            if (rolesAdded.Count > 0) { // roles were added
                await server.UpdateContingentRoles_AddedAsync(user, beforeUser, rolesAfter);
            }

            if (rolesRemoved.Count > 0) { // the user had roles removed
                await server.UpdateContingentRoles_RemovedAsync(user, beforeUser, rolesRemoved, botTriggered);
            }

            if (botTriggered) {
                return;
            }

            if (server.AutoRolePersist && rolesAdded.Count > 0) {
                await user.AddRolesPersistedAsync(rolesAdded, null);
            }

            if (rolesRemoved.Count > 0) {
                await user.RemoveRolesPersistedAsync(rolesRemoved);
            }     
        }

        private static async Task Client_UserJoined(SocketGuildUser socketUser) {
            User user = await socketUser.Guild.GetUserAsync(socketUser.Id);
            IEnumerable<ulong> rolesPersisted = user.GetRolesPersistedIds();

            if (rolesPersisted.Any()) { // this user has role persists on this server
                IEnumerable<ulong> persistlessContingentRoles = (await user.GetActiveContingentRoleIds()).Except(rolesPersisted);
                foreach(ulong contingentRole in persistlessContingentRoles) {
                    await user.RemoveActiveContingentRoleAsync(contingentRole);
                }

                IEnumerable<ulong> userRoleIds = socketUser.Guild.Roles.Where(role => role.Position < socketUser.Guild.CurrentUser.Hierarchy)
                    .Select(role => role.Id).Intersect(rolesPersisted)
                    .Except(await user.GetContingentRolesRemoved());

                if (userRoleIds.Any()) {
                    await socketUser.AddRolesAsync(userRoleIds);
                }
            }
        }
    }
}
