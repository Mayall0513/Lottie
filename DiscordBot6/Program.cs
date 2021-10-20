using Discord;
using Discord.WebSocket;
using DiscordBot6.Database;
using DiscordBot6.PhraseRules;
using DiscordBot6.Users;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public static class Program {
        private static DiscordSocketClient discordClient;

        public static ulong BotAccountId { get; private set; }

        public static void Main(string[] arguments) {
            discordClient = new DiscordSocketClient();
            discordClient.Ready += DiscordClient_Ready;
            discordClient.MessageReceived += DiscordClient_MessageReceived;
            discordClient.UserVoiceStateUpdated += DiscordClient_UserVoiceStateUpdated;
            discordClient.GuildMemberUpdated += DiscordClient_GuildMemberUpdated;
            discordClient.UserJoined += DiscordClient_UserJoined;

            CreateBot();
            Console.Read();
        }

        private static async void CreateBot() {
            await discordClient.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["BotToken"], true);
            await discordClient.StartAsync();
        }

        private static Task DiscordClient_Ready() {
            BotAccountId = discordClient.CurrentUser.Id;
            return Task.CompletedTask;
        }

        private static async Task DiscordClient_MessageReceived(SocketMessage socketMessage) {
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

        private static async Task DiscordClient_UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState beforeVoiceState, SocketVoiceState afterVoiceState) {
            if (afterVoiceState.VoiceChannel == null) { // they just left
                return; // there's nothing we want to do if a user leaves the server
            }

            SocketGuildUser socketGuildUser = (socketUser as SocketGuildUser);
            Server server = await Server.GetServerAsync(socketGuildUser.Guild.Id);

            if ((!server.AutoMutePersist && !server.AutoDeafenPersist) || server.CheckAndRemoveVoiceStatusUpdated(socketGuildUser.Id)) { // this server does not want automatic mute or deafen persist OR this is an event that we triggered and should ignore
                return;
            }

            if (beforeVoiceState.VoiceChannel == null) { // they just joined
                UserSettings userSettings = await server.GetUserSettingsAsync(socketUser.Id);

                if (userSettings != null) {
                    if (!afterVoiceState.IsMuted && userSettings.MutePersisted) { // user is not muted and should be
                        server.TryAddVoiceStatusUpdated(socketUser.Id);
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                    }

                    if (!afterVoiceState.IsDeafened && userSettings.DeafenPersisted) { // user is not deafened and should be
                        server.TryAddVoiceStatusUpdated(socketUser.Id);
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Deaf = true; });
                    }
                }
            }

            else { // something else happened
                bool muteChanged = beforeVoiceState.IsMuted != afterVoiceState.IsMuted;
                bool deafenChanged = beforeVoiceState.IsDeafened != afterVoiceState.IsDeafened;

                if ((server.AutoMutePersist && muteChanged) || (server.AutoDeafenPersist && deafenChanged)) { // the user was (un)muted or (un)deafened AND the server wants to automatically persist the change
                    UserSettings userSettings = await server.GetUserSettingsAsync(socketUser.Id);

                    userSettings.MutePersisted = afterVoiceState.IsMuted;
                    userSettings.DeafenPersisted = afterVoiceState.IsDeafened;

                    await server.SetUserSettingsAsync(socketGuildUser.Id, userSettings);
                }
            }
        }

        private static async Task DiscordClient_GuildMemberUpdated(SocketGuildUser beforeUser, SocketGuildUser afterUser) {
            if (beforeUser.Roles.Count == afterUser.Roles.Count) { // this was not caused by roles being taken or added
                return;
            }

            Server server = await Server.GetServerAsync(beforeUser.Guild.Id);
            if (!server.AutoRolePersist || server.CheckAndRemoveRoleUpdated(beforeUser.Id)) { // this server does not want automatic role persists OR this is an event that we triggered and should ignore
                return;
            }

            if (beforeUser.Roles.Count > afterUser.Roles.Count) { // role taken
                SocketRole roleRemoved = beforeUser.Roles.Except(afterUser.Roles).First();
                await server.RemoveRolePersistAsync(beforeUser.Id, roleRemoved.Id);
            }

            else { // role added
                SocketRole roleAdded = afterUser.Roles.Except(beforeUser.Roles).First();
                await server.AddRolePersistAsync(beforeUser.Id, roleAdded.Id);
            }
        }

        private static async Task DiscordClient_UserJoined(SocketGuildUser user) {
            ulong[] rolePersists = await Repository.GetRolePersistsAsync(user.Guild.Id, user.Id);

            if (rolePersists != null && rolePersists.Length != 0) { // this user has role persists on this server
                Server server = await Server.GetServerAsync(user.Guild.Id);
                server.TryAddRoleUpdated(user.Id);

                await user.AddRolesAsync(rolePersists);
            }
        }
    }
}
