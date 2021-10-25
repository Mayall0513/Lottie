using Discord;
using DiscordBot6.Constraints;
using DiscordBot6.ContingentRoles;
using DiscordBot6.Database;
using DiscordBot6.PhraseRules;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot6 {
    public static class GuildExtensions {
        public static async Task<Server> GetServerAsync(this IGuild guild) {
            return await Server.GetServerAsync(guild.Id);
        }

        public static async Task<User> GetUserAsync(this IGuild guild, ulong id) {
            return await (await GetServerAsync(guild)).GetUserAsync(id);
        }
    }

    public sealed class Server {
        private static readonly ConcurrentDictionary<ulong, Server> serverCache = new ConcurrentDictionary<ulong, Server>();

        private readonly ConcurrentDictionary<ulong, User> users = new ConcurrentDictionary<ulong, User>();

        private IReadOnlyCollection<PhraseRule> phraseRules;
        private IReadOnlyCollection<ContingentRole> contingentRoles;

        public ulong Id { get; }

        public ulong LogChannelId => logChannelId.Value;
        public bool HasLogChannel => logChannelId.HasValue;

        private char? commandPrefix;
        private ulong? logChannelId;

        public bool AutoMutePersist { get; }
        public bool AutoDeafenPersist { get; }
        public bool AutoRolePersist { get; }

        private readonly HashSet<ulong> commandChannels = new HashSet<ulong>();

        private CRUConstraints tempMuteConstraints;
        private CRUConstraints muteConstraints;
        private CRUConstraints giveRolesConstraints;

        public Server(ulong id, char? commandPrefix, ulong? logChannelId, bool autoMutePersist, bool autoDeafenPersist, bool autoRolePersist, IEnumerable<ulong> commandChannels) {
            Id = id;

            this.commandPrefix = commandPrefix;
            this.logChannelId = logChannelId;

            AutoMutePersist = autoMutePersist;
            AutoDeafenPersist = autoDeafenPersist;
            AutoRolePersist = autoRolePersist;

            this.commandChannels = new HashSet<ulong>(commandChannels);
        }

        public static async Task<Server> GetServerAsync(ulong id) {
            if (!serverCache.ContainsKey(id)) {
                Server newServer = await Repository.GetServerAsync(id);
                if (newServer == null) {
                    newServer = new Server(id, null, null, true, true, false, Enumerable.Empty<ulong>());
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


        public async Task<IEnumerable<PhraseRule>> GetPhraseRuleSetsAsync() {
            if (phraseRules == null) {
                phraseRules = await Repository.GetPhraseRulesAsync(Id) as IReadOnlyCollection<PhraseRule>;
            }

            return phraseRules;
        }

        public async Task<IEnumerable<ContingentRole>> GetContingentRolesAsync() {
            if (contingentRoles == null) {
                contingentRoles = await Repository.GetContingentRulesAsync(Id) as IReadOnlyCollection<ContingentRole>;
            }

            return contingentRoles;
        }


        public async Task<User> GetUserAsync(ulong id) {
            if (!users.ContainsKey(id)) {
                User user = await Repository.GetUserAsync(Id, id);

                if (user == null) {
                    user = new User(id, false, false) {
                        Parent = this
                    };

                    await Repository.AddOrUpdateUserAsync(user);
                }

                else {
                    user.Parent = this;
                }
                
                users.TryAdd(id, user);

                return user;
            }

            return users[id];
        }

        public async Task SetUserAsync(ulong id, User user) {
            if (!users.ContainsKey(id)) {
                await GetUserAsync(id);
            }

            if (users.ContainsKey(id)) {
                users[id] = user;
                await Repository.AddOrUpdateUserAsync(user);
            }
        }


        public async Task<bool> UserMayTempMute(ulong userId, IEnumerable<ulong> roleIds) {
            if (tempMuteConstraints == null) {
                tempMuteConstraints = await Repository.GetConstraints(Id, Repository.ConstraintIntents.TEMPMUTE);
            }

            return tempMuteConstraints.Matches(null, roleIds, userId);
        }

        public async Task<bool> UserMayMute(ulong userId, IEnumerable<ulong> roleIds) {
            if (muteConstraints == null) {
                muteConstraints = await Repository.GetConstraints(Id, Repository.ConstraintIntents.MUTE);
            }

            return muteConstraints.Matches(null, roleIds, userId);
        }

        public async Task<bool> UserMayGiveRoles(ulong userId, IEnumerable<ulong> roleIds) {
            if (giveRolesConstraints == null) {
                giveRolesConstraints = await Repository.GetConstraints(Id, Repository.ConstraintIntents.GIVEROLES);
            }

            return giveRolesConstraints.Matches(null, roleIds, userId);
        }


        public bool IsCommandChannel(ulong channelId) {
            return commandChannels.Count == 0 || commandChannels.Contains(channelId);
        }

        public char GetCommandPrefix() {
            return commandPrefix ?? DiscordBot6.DefaultCommandPrefix;
        }
    }
}
