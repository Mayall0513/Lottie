using System.Collections.Generic;

namespace DiscordBot6.Database.Models {
    public sealed class ServerModel : IModelFor<Server> {
        public ulong Id { get; set; }

        public string CommandPrefix { get; set; }
        public ulong? LogChannelId { get; set; }

        public bool AutoMutePersist { get; set; }
        public bool AutoDeafenPersist { get; set; }
        public bool AutoRolePersist { get; set; }

        public HashSet<ulong> CommandChannels { get; set; } = new HashSet<ulong>();

        public Server CreateConcrete() {
            return new Server(Id, CommandPrefix, LogChannelId, AutoMutePersist, AutoDeafenPersist, AutoRolePersist, CommandChannels);
        }
    }
}
