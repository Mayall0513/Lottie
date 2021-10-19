using System;
using System.Collections.Generic;
using System.Text;

using DiscordBot6.Database.Models;

namespace DiscordBot6.Users {
    public sealed class UserSettings {
        public bool MutePersisted { get; set; }
        public bool DeafenPersisted { get; set; }
        
        public UserSettings(bool mutePersisted, bool deafenPersisted) {
            MutePersisted = mutePersisted;
            DeafenPersisted = deafenPersisted;
        }

        public static UserSettings FromModel(UserSettingsModel userSettingsModel) {
            return new UserSettings(userSettingsModel.MutePersisted, userSettingsModel.DeafenPersisted);
        }
    }
}
