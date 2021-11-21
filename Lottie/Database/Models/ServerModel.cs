using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Lottie.Database.Models {
    public sealed class ServerModel : IModelFor<Server> {
        public ulong Id { get; set; }

        public string CommandPrefix { get; set; }
        public ulong? LogChannelId { get; set; }
        public ulong? JailRoleId { get; set; }

        public bool AutoMutePersist { get; set; }
        public bool AutoDeafenPersist { get; set; }
        public bool AutoRolePersist { get; set; }

        public HashSet<ulong> CommandChannels { get; set; } = new HashSet<ulong>();

        public Dictionary<uint, List<string>> CustomMessages { get; set; } = new Dictionary<uint, List<string>>();

        public Server CreateConcrete() {
            ConcurrentDictionary<PresetMessageTypes, string[]> customMessages = new ConcurrentDictionary<PresetMessageTypes, string[]>();
            foreach (uint customMessageType in CustomMessages.Keys) {
                customMessages.TryAdd((PresetMessageTypes) customMessageType, CustomMessages[customMessageType].ToArray());
            }

            return new Server(Id, CommandPrefix, LogChannelId, JailRoleId, AutoMutePersist, AutoDeafenPersist, AutoRolePersist, CommandChannels, customMessages);
        }
    }
}
