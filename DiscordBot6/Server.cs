using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using DiscordBot6.Database;
using DiscordBot6.PhraseRules;
using DiscordBot6.Users;

namespace DiscordBot6 {
    public sealed class Server {
        private static readonly ConcurrentDictionary<ulong, Server> serverCache = new ConcurrentDictionary<ulong, Server>();

        public ulong ServerId { get; }

        private ConcurrentDictionary<ulong, UserSettings> users = new ConcurrentDictionary<ulong, UserSettings>();
        private PhraseRule[] phraseRules;

        public Server(ulong id) {
            ServerId = id;
        }

        public async Task<PhraseRule[]> GetPhraseRuleSetsAsync() {
            if (phraseRules == null) {
                phraseRules = await Repository.GetPhraseRules(ServerId);
            }

            return phraseRules;
        }

        public async Task<UserSettings> GetUserSettings(ulong id) {
            if (!users.ContainsKey(id)) {
                UserSettings newUser = await Repository.GetUserSettings(ServerId, id);
                if (newUser != null) {
                    users.TryAdd(id, newUser);
                }

                return newUser;
            }

            users.TryGetValue(id, out UserSettings userSettings);
            return userSettings;
        }

        public async Task SetUserSettings(ulong id, UserSettings userSettings) {
            if (users.ContainsKey(id)) {
                users[id] = userSettings;
                await Repository.UpdateUserSettings(ServerId, id, userSettings);
            }

            else {
                users.TryAdd(id, userSettings);

                if ((await Repository.GetUserSettings(ServerId, id)) == null) {
                    await Repository.AddUserSettings(ServerId, id, userSettings);
                }

                else {
                    await Repository.UpdateUserSettings(ServerId, id, userSettings);
                }
            }
        }

        public static Server GetServer(ulong id) {
            if (!serverCache.ContainsKey(id)) {
                Server newServer = new Server(id);
                serverCache.TryAdd(id, newServer);

                return newServer;
            }

            serverCache.TryGetValue(id, out Server server);
            return server;
        }

        public static bool ResetServerCache(ulong id) {
            return serverCache.TryRemove(id, out _);
        }
    }
}
