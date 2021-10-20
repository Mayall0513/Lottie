using System.Collections.Generic;

namespace DiscordBot6.Users {
    public sealed class UserSettings {
        public bool MutePersisted { get; set; }
        public bool DeafenPersisted { get; set; }

        public HashSet<ulong> RolesPersisted { get; set; } = new HashSet<ulong>();

        public UserSettings(bool mutePersisted, bool deafenPersisted) {
            MutePersisted = mutePersisted;
            DeafenPersisted = deafenPersisted;
        }
    }
}
