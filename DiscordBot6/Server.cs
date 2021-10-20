using DiscordBot6.Database;
using DiscordBot6.PhraseRules;
using DiscordBot6.Users;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public sealed class Server {
        private static readonly ConcurrentDictionary<ulong, Server> serverCache = new ConcurrentDictionary<ulong, Server>();

        private readonly HashSet<ulong> userVoicesStatusUpdated = new HashSet<ulong>();
        private readonly HashSet<ulong> userRolesUpdated = new HashSet<ulong>();
        private readonly ConcurrentDictionary<ulong, UserSettings> users = new ConcurrentDictionary<ulong, UserSettings>();
        private PhraseRule[] phraseRules;

        public ulong Id { get; }

        public bool AutoMutePersist { get; }
        public bool AutoDeafenPersist { get; }
        public bool AutoRolePersist { get; }

        public Server(ulong id, bool autoMutePersist, bool autoDeafenPersist, bool autoRolePersist) {
            Id = id;

            AutoMutePersist = autoMutePersist;
            AutoDeafenPersist = autoDeafenPersist;
            AutoRolePersist = autoRolePersist;
        }

        public async Task<PhraseRule[]> GetPhraseRuleSetsAsync() {
            if (phraseRules == null) {
                phraseRules = await Repository.GetPhraseRulesAsync(Id);
            }

            return phraseRules;
        }

        public async Task<UserSettings> GetUserSettingsAsync(ulong id) {
            if (!users.ContainsKey(id)) {
                UserSettings newUser = await Repository.GetUserSettingsAsync(Id, id);
                if (newUser == null) {
                    newUser = new UserSettings(false, false);
                    await Repository.AddOrUpdateUserSettingsAsync(Id, id, newUser);
                }

                users.TryAdd(id, newUser);
                return newUser;
            }

            return users[id];
        }

        public async Task SetUserSettingsAsync(ulong id, UserSettings userSettings) {
            if (!users.ContainsKey(id)) {
                await GetUserSettingsAsync(id);
            }

            if (users.ContainsKey(id)) {
                users[id] = userSettings;
                await Repository.AddOrUpdateUserSettingsAsync(Id, id, userSettings);
            }
        }

        public static async Task<Server> GetServerAsync(ulong id) {
            if (!serverCache.ContainsKey(id)) {
                Server newServer = await Repository.GetServerAsync(id);
                if (newServer == null) {
                    newServer = new Server(id, true, true, false);
                    await Repository.AddOrUpdateServerAsync(newServer);
                }

                serverCache.TryAdd(id, newServer);
                return newServer;
            }

            return serverCache[id];
        }

        public static bool ResetServerCache(ulong id) {
            return serverCache.TryRemove(id, out _);
        }

        public async Task AddRolePersistAsync(ulong userId, ulong roleId) {
            UserSettings userSettings = await GetUserSettingsAsync(userId);

            if (userSettings.RolesPersisted.Add(roleId)) { // only add to db if adding it to the cache was successful - free duplicate checking!
                await Repository.AddRolePersistAsync(Id, userId, roleId);
            }
        }

        public async Task RemoveRolePersistAsync(ulong userId, ulong roleId) {
            UserSettings userSettings = await GetUserSettingsAsync(userId);

            if (userSettings.RolesPersisted.Remove(roleId)) { // only remove from db if removing it from the cache was successful - free duplicate checking!
                await Repository.RemoveRolePersistAsync(Id, userId, roleId);
            }
        }

        public bool TryAddVoiceStatusUpdated(ulong userId) {
            return userVoicesStatusUpdated.Add(userId);
        }

        public bool CheckAndRemoveVoiceStatusUpdated(ulong userId) {
            if (userVoicesStatusUpdated.Contains(userId)) {
                userVoicesStatusUpdated.Remove(userId);
                return true;
            }

            return false;
        }

        public bool TryAddRoleUpdated(ulong userId) {
            return userRolesUpdated.Add(userId);
        }

        public bool CheckAndRemoveRoleUpdated(ulong userId) {
            if (userRolesUpdated.Contains(userId)) {
                userRolesUpdated.Remove(userId);
                return true;
            }

            return false;
        }
    }
}
