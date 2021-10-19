using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using DiscordBot6.PhraseRules;
using DiscordBot6.Users;

namespace DiscordBot6 {
    public static class Program {
        private static HashSet<ulong> accountsMutedOrDeafened = new HashSet<ulong>();

        private static DiscordSocketClient discordClient;

        public static ulong BotAccountId { get; private set; }

        public static void Main(string[] arguments) {
            discordClient = new DiscordSocketClient();
            discordClient.Ready += DiscordClient_Ready;
            discordClient.MessageReceived += DiscordClient_MessageReceived;
            discordClient.UserVoiceStateUpdated += DiscordClient_UserVoiceStateUpdated;

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
            Server server = Server.GetServer(socketGuildChannel.Guild.Id);
            PhraseRule[] phraseRules = await server.GetPhraseRuleSetsAsync();

            foreach (PhraseRule phraseRule in phraseRules) { 
                if (phraseRule.CanApply(socketMessage) && phraseRule.Matches(socketMessage.Content)) {
                    await socketMessage.Channel.DeleteMessageAsync(socketMessage);
                    break;
                }
            }
        }

        private static async Task DiscordClient_UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState beforeVoiceState, SocketVoiceState afterVoiceState) {
            if (afterVoiceState.VoiceChannel == null) { // they just left
                return;
            }


            SocketGuildUser socketGuildUser = (socketUser as SocketGuildUser);
            await foreach (var auditLogEntry in socketGuildUser.Guild.GetAuditLogsAsync(1, null, null, socketUser.Id, ActionType.MemberUpdated)) {

            }


            if (accountsMutedOrDeafened.Contains(socketUser.Id)) { // we just muted or deafened this user - we don't care
                accountsMutedOrDeafened.Remove(socketUser.Id);
                return;
            }

            if (beforeVoiceState.VoiceChannel == null) { // they just joined
                
                Server server = Server.GetServer(socketGuildUser.Guild.Id);
                UserSettings userSettings = await server.GetUserSettings(socketUser.Id);

                if (userSettings != null) {
                    if (!afterVoiceState.IsMuted && userSettings.MutePersisted) {
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Mute = true; });
                        accountsMutedOrDeafened.Add(socketGuildUser.Id);
                    }

                    if (!afterVoiceState.IsDeafened && userSettings.DeafenPersisted) {
                        await socketGuildUser.ModifyAsync(userProperties => { userProperties.Deaf = true; });
                        accountsMutedOrDeafened.Add(socketGuildUser.Id);
                    }
                }
            }

            else {
                bool muteChanged = beforeVoiceState.IsMuted != afterVoiceState.IsMuted;
                bool deafenChanged = beforeVoiceState.IsDeafened != afterVoiceState.IsDeafened;

                if (muteChanged || deafenChanged) {
                    SocketGuildUser socketGuildUser = (socketUser as SocketGuildUser);
                    Server server = Server.GetServer(socketGuildUser.Guild.Id);
                    UserSettings userSettings = await server.GetUserSettings(socketUser.Id) ?? new UserSettings(false, false);

                    if (muteChanged) {
                        userSettings.MutePersisted = afterVoiceState.IsMuted;
                    }

                    else {
                        userSettings.DeafenPersisted = afterVoiceState.IsDeafened;
                    }

                    await server.SetUserSettings(socketGuildUser.Id, userSettings);
                }
            }
        }
    }
}
