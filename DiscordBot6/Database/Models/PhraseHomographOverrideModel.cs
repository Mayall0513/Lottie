using System.Collections.Generic;

namespace DiscordBot6.Database.Models {
    public sealed class PhraseHomographOverrideModel {
        public int OverrideType { get; set; }
        public string Pattern { get; set; }

        public List<string> Homographs { get; set; } = new List<string>();

        public PhraseHomographOverrideModel(int overrideType, string pattern) {
            Pattern = pattern;
            OverrideType = overrideType;
        }
    }
}
