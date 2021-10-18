﻿using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using DiscordBot6.PhraseRules;

namespace DiscordBot6 {
    public static class Program {
        private static Dictionary<ulong, ServerInformation> servers = new Dictionary<ulong, ServerInformation>();
        private static DiscordSocketClient discordClient;

        public static ulong BotAccountId { get; private set; }

        public static void Main(string[] arguments) {
            discordClient = new DiscordSocketClient();
            discordClient.Ready += DiscordClient_Ready;
            discordClient.MessageReceived += DiscordClient_MessageReceived;

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

        private static ServerInformation GetServer(ulong id) {
            if (!servers.ContainsKey(id)) {
                servers.Add(id, new ServerInformation(id));
            }

            return servers[id];
        }

        private static async Task DiscordClient_MessageReceived(SocketMessage socketMessage) {
            SocketGuildChannel socketGuildChannel = (socketMessage.Channel as SocketGuildChannel);
            ServerInformation serverInformation = GetServer(socketGuildChannel.Guild.Id);
            PhraseRule[] phraseRules = await serverInformation.GetPhraseRuleSetsAsync();

            foreach (PhraseRule phraseRule in phraseRules) { 
                if (phraseRule.CanApply(socketMessage) && phraseRule.Matches(socketMessage.Content)) {
                    await socketMessage.Channel.DeleteMessageAsync(socketMessage);
                    break;
                }
            }
        }
    }
}
