using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot6.ContingentRoles;
using DiscordBot6.Database;
using DiscordBot6.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public static class DiscordBot6 {
        public static DiscordSocketClient Client { get; private set; }
        public static CommandService commandService { get; private set; }

        public static IUser BotAccount => Client.CurrentUser;
        public static ulong BotAccountId => Client.CurrentUser.Id;

        public const char DiscordNewLine = '\n';
        public const char DefaultCommandPrefix = '+';

        public static async Task Main(string[] _arguments) {
            DiscordShardedClient shardClient = new DiscordShardedClient();

            commandService = new CommandService();
            await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            shardClient.ShardReady += Client_ShardReady;
            shardClient.MessageReceived += Client_MessageReceived;
            shardClient.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
            shardClient.GuildMemberUpdated += Client_GuildMemberUpdated;
            shardClient.UserJoined += Client_UserJoined;

            await shardClient.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["BotToken"], true);
            await shardClient.StartAsync();
            await Task.Delay(-1);
        }

        private static async Task Client_ShardReady(DiscordSocketClient client) {
            Client = client;

            await foreach (MutePersist mutePersist in Repository.GetMutePersistsAllAsync(client.Guilds.Select(guild => guild.Id))) {
                Server server = await Server.GetServerAsync(mutePersist.ServerId);
                User user = await server.GetUserAsync(mutePersist.UserId);

                user.PrecacheMutePersisted(mutePersist);
            }
        }

        private static async Task Client_MessageReceived(SocketMessage socketMessage) {
            if (!(socketMessage is SocketUserMessage socketUserMessage)) {
                return;
            }

            if (socketMessage.Channel is SocketGuildChannel socketGuildChannel) { // message was sent in a server
                Server server = await socketGuildChannel.Guild.GetServerAsync();

                if (socketMessage.Author.Id != BotAccountId && server.IsCommandChannel(socketMessage.Channel.Id)) { // message was not sent by the bot and was sent in a command channel
                    int argumentIndex = 0;

                    if (socketUserMessage.HasCharPrefix(server.GetCommandPrefix(), ref argumentIndex)) {
                        SocketCommandContext commandContext = new SocketCommandContext(Client, socketUserMessage);
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

        private static async Task Client_UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState beforeVoiceState, SocketVoiceState afterVoiceState) {
            if (afterVoiceState.VoiceChannel == null) { // they just left
                return; // there's nothing we want to do if a user leaves the channel
            }

            if (socketUser is SocketGuildUser socketGuildUser) { // this happened in a server's voice channel
                Server server = await socketGuildUser.Guild.GetServerAsync();
                User user = await server.GetUserAsync(socketUser.Id);

                if (user.DecrementVoiceStatusUpdated()) { // this is an event that we triggered and therefore should ignore
                    return;
                }

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

                        user.GlobalMutePersisted = afterVoiceState.IsMuted;
                    }

                    if (server.AutoDeafenPersist && deafenChanged) { // the user was (un)muted or (un)deafened AND the server wants to automatically persist the change
                        user.GlobalDeafenPersisted = afterVoiceState.IsDeafened;
                    }

                    await server.SetUserAsync(socketGuildUser.Id, user);
                }

                if ((beforeVoiceState.VoiceChannel != afterVoiceState.VoiceChannel) && !user.GlobalMutePersisted) { // the user moved channels AND they are not globally mute persisted
                    IEnumerable<ulong> mutePersists = user.GetMutesPersisted().Select(mutePersist => mutePersist.ChannelId); // get channel specific mute persists
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

            if (user.DecrementRolesUpdated()) { // this is an event that we triggered and therefore should ignore
                return;
            }

            if (rolesAdded.Count > 0) { // roles were added
                IEnumerable<ContingentRole> contingentRoles = await server.GetContingentRolesAsync();
                HashSet<ulong> rolesToRemove = new HashSet<ulong>();

                foreach (ContingentRole contingentRole in contingentRoles) {
                    if (rolesAdded.Contains(contingentRole.RoleId)) { // the user received a role that disallows other roles
                        IEnumerable<ulong> commonRoles = contingentRole.ContingentRoles.Intersect(rolesAfter);

                        if (commonRoles.Any()) { // the user has roles they're no longer allowed to have
                            rolesToRemove.UnionWith(commonRoles);
                            await user.AddContingentRolesRemovedAsync(contingentRole.RoleId, commonRoles);
                        }
                    }
                }

                foreach (ulong currentRole in rolesAfter) {
                    foreach (ContingentRole contingentRole in contingentRoles) {
                        if (contingentRole.RoleId == currentRole) { // the user was given a role they're not allowed to have because of contingent roles they have
                            rolesToRemove.UnionWith(contingentRole.ContingentRoles.Intersect(rolesAdded));
                            break; // each contingent role can only match with one role the user has - no point in checking any more
                        }
                    }
                }

                if (rolesToRemove.Count > 0) { // there are roles we need to remove
                    user.IncrementRolesUpdated();
                    await beforeUser.RemoveRolesAsync(rolesToRemove);

                    if (server.AutoRolePersist) {
                        await user.RemoveRolesPersistedAsync(rolesToRemove);
                    }
                }
            }

            if (rolesRemoved.Count > 0) { // the user had roles removed
                ConcurrentDictionary<ulong, HashSet<ulong>> contingentRolesRemoved = await user.GetContingentRolesRemovedAsync();

                foreach (ulong bannedRole in contingentRolesRemoved.Keys) {
                    if (rolesRemoved.Contains(bannedRole)) { // the user had one of their roles removed
                        HashSet<ulong> rolesToAdd = contingentRolesRemoved[bannedRole];

                        if (rolesToAdd.Count > 0) { // there are roles that were taken when the user was given a contingent role. we need to give those back
                            user.IncrementRolesUpdated();

                            await beforeUser.AddRolesAsync(rolesToAdd);
                            await user.RemoveContingentRoleRemovedAsync(bannedRole);

                            if (server.AutoRolePersist) {
                                await user.AddRolesPersistedAsync(rolesToAdd);
                            }
                        }
                    }
                }
            }

            if (!server.AutoRolePersist) { // server doesn't want automatic role persists
                return;
            }

            if (rolesAdded.Count > 0) {
                await user.AddRolesPersistedAsync(rolesAdded);
            }

            if (rolesRemoved.Count > 0) {
                await user.RemoveRolesPersistedAsync(rolesRemoved);
            }     
        }

        private static async Task Client_UserJoined(SocketGuildUser socketUser) {
            User user = await socketUser.Guild.GetUserAsync(socketUser.Id);
            IEnumerable<ulong> rolesPersisted = await user.GetRolesPersistedAsync();

            if (rolesPersisted.Any()) { // this user has role persists on this server
                user.IncrementRolesUpdated();
                await socketUser.AddRolesAsync(rolesPersisted);
            }
        }
    }
}
