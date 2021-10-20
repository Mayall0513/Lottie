using DiscordBot6.Users;

namespace DiscordBot6.Database.Models {
    public sealed class UserSettingsModel : IModelFor<UserSettings> {
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public bool MutePersisted { get; set; }
        public bool DeafenPersisted { get; set; }

        public UserSettings CreateConcrete() {
            return new UserSettings(MutePersisted, DeafenPersisted);
        }
    }
}
