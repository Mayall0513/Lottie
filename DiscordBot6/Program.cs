using Discord;
using Discord.WebSocket;
using DiscordBot6.ContingentRoles;
using DiscordBot6.PhraseRules;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public static class Program {
        private static DiscordSocketClient client;

        public static ulong BotAccountId { get; private set; }

        public static async Task Main(string[] arguments) {
            client = new DiscordSocketClient();

            client.Ready += Client_Ready;
            client.MessageReceived += Client_MessageReceived;
            client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;
            client.GuildMemberUpdated += Client_GuildMemberUpdated;
            client.UserJoined += Client_UserJoined;

            await client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["BotToken"], true);
            await client.StartAsync();
            await Task.Delay(-1);
        }

        private static Task Client_Ready() {
            BotAccountId = client.CurrentUser.Id;
            return Task.CompletedTask;
        }

        private static async Task Client_MessageReceived(SocketMessage socketMessage) {
            SocketGuildChannel socketGuildChannel = (socketMessage.Channel as SocketGuildChannel);
            Server server = await Server.GetServerAsync(socketGuildChannel.Guild.Id);
            PhraseRule[] phraseRules = await server.GetPhraseRuleSetsAsync();

            // this is unfinished - i want to do a thing where the user can decide what happens. 
            // just a placeholder for the moment!
            foreach (PhraseRule phraseRule in phraseRules) {
                if (phraseRule.CanApply(socketMessage) && phraseRule.Matches(socketMessage.Content)) {
                    await socketMessage.Channel.DeleteMessageAsync(socketMessage);
                    break;
                }
            }
        }

        private static async Task Client_UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState beforeVoiceState, SocketVoiceState afterVoiceState) {
            if (afterVoiceState.VoiceChannel == null) { // they just left
                return; // there's nothing we want to do if a user leaves the channel
            }

            SocketGuildUser socketGuildUser = (socketUser as SocketGuildUser);
            Server server = await Server.GetServerAsync(socketGuildUser.Guild.Id);

            if ((!server.AutoMutePersist && !server.AutoDeafenPersist) || server.CheckAndRemoveVoiceStatusUpdated(socketGuildUser.Id)) { // this server does not want automatic mute or deafen persist OR this is an event that we triggered and should ignore
                return;
            }

            if (beforeVoiceState.VoiceChannel == null) { // they just joined
                User user = await server.GetUserAsync(socketUser.Id);

                if (user != null) {
                    if (!afterVoiceState.IsMuted && user.MutePersisted) { // user is not muted and should be
                        server.TryAddVoiceStatusUpdated(socketUser.Id);
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }

                    if (!afterVoiceState.IsDeafened && user.DeafenPersisted) { // user is not deafened and should be
                        server.TryAddVoiceStatusUpdated(socketUser.Id);
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Deaf = true; });
                    }
                }
            }

            else { // something else happened
                bool muteChanged = beforeVoiceState.IsMuted != afterVoiceState.IsMuted;
                bool deafenChanged = beforeVoiceState.IsDeafened != afterVoiceState.IsDeafened;

                if ((server.AutoMutePersist && muteChanged) || (server.AutoDeafenPersist && deafenChanged)) { // the user was (un)muted or (un)deafened AND the server wants to automatically persist the change
                    User userSettings = await server.GetUserAsync(socketUser.Id);

                    userSettings.MutePersisted = afterVoiceState.IsMuted;
                    userSettings.DeafenPersisted = afterVoiceState.IsDeafened;

                    await server.SetUserSettingsAsync(socketGuildUser.Id, userSettings);
                }
            }
        }

        private static async Task Client_GuildMemberUpdated(SocketGuildUser beforeUser, SocketGuildUser afterUser) {
            HashSet<ulong> rolesAfter = new HashSet<ulong>(afterUser.Roles.Select(role => role.Id));

            HashSet<ulong> rolesAdded = new HashSet<ulong>(rolesAfter);
            HashSet<ulong> rolesRemoved = new HashSet<ulong>(beforeUser.Roles.Select(role => role.Id));

            rolesAdded.ExceptWith(rolesRemoved);
            rolesRemoved.ExceptWith(rolesAfter);

            if (rolesAdded.Count == 0 && rolesRemoved.Count == 0) { // no roles were changed
                return; // there's nothing we want to do
            }

            Server server = await Server.GetServerAsync(beforeUser.Guild.Id);
            User user = await server.GetUserAsync(beforeUser.Id);

            if (server.CheckAndRemoveRoleUpdated(user.Id)) { // 
                return;
            }

            if (rolesAdded.Count > 0) {
                ContingentRole[] contingentRoles = await server.GetContingentRolesAsync();
                HashSet<ulong> rolesToRemove = new HashSet<ulong>();

                foreach (ContingentRole contingentRole in contingentRoles) {
                    if (rolesAdded.Contains(contingentRole.RoleId)) {
                        IEnumerable<ulong> commonRoles = contingentRole.ContingentRoles.Intersect(rolesAfter);

                        if (commonRoles.Any()) {
                            rolesToRemove.UnionWith(commonRoles);
                            await user.AddContingentRolesRemovedAsync(contingentRole.RoleId, commonRoles);
                        }
                    }
                }

                foreach (ulong currentRole in rolesAfter) {
                    foreach (ContingentRole contingentRole in contingentRoles) {
                        if (contingentRole.RoleId == currentRole) {
                            rolesToRemove.UnionWith(contingentRole.ContingentRoles.Intersect(rolesAdded));
                            break;
                        }
                    }
                }

                if (rolesToRemove.Count > 0) {
                    server.TryAddRoleUpdated(user.Id);
                    await beforeUser.RemoveRolesAsync(rolesToRemove);

                    if (server.AutoRolePersist) {
                        await user.RemoveRolesPersistedAsync(rolesToRemove);
                    }
                }
            }

            if (rolesRemoved.Count > 0) {
                ConcurrentDictionary<ulong, HashSet<ulong>> contingentRolesRemoved = await user.GetContingentRolesRemoved();

                foreach (ulong bannedRole in contingentRolesRemoved.Keys) {
                    if (rolesRemoved.Contains(bannedRole)) {
                        HashSet<ulong> rolesToAdd = contingentRolesRemoved[bannedRole];

                        if (rolesToAdd.Count > 0) {
                            server.TryAddRoleUpdated(user.Id);

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
                return; // there's nothing else we need to do
            }

            if (rolesAdded.Count > 0) {
                await user.AddRolesPersistedAsync(rolesAdded);
            }

            if (rolesRemoved.Count > 0) {
                await user.RemoveRolesPersistedAsync(rolesRemoved);
            }     
        }

        private static async Task Client_UserJoined(SocketGuildUser socketUser) {
            Server server = await Server.GetServerAsync(socketUser.Guild.Id);
            User user = await server.GetUserAsync(socketUser.Id);
            IEnumerable<ulong> rolesPersisted = await user.GetRolesPersistedAsync();

            if (rolesPersisted.Any()) { // this user has role persists on this server
                server.TryAddRoleUpdated(socketUser.Id);
                await socketUser.AddRolesAsync(rolesPersisted);
            }
        }
    }
}
