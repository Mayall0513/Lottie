using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot6.Database.Models {
    public sealed class UserSettingsModel { // this isn't used yet but it will be later so the model stays
        public bool MutePersisted { get; set; }
        public bool DeafenPersisted { get; set; }
    }
}
