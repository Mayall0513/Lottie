using System.Collections.Generic;

namespace DiscordBot6.Database.Models.PhraseRules {
    public sealed class PhraseSubstringModifierModel {
        public int ModifierType { get; set; }
        public int SubstringStart { get; set; }
        public int SubstringEnd { get; set; }

        public List<string> Data { get; set; } = new List<string>();

        public PhraseSubstringModifierModel(int modifierType, int substringStart, int substringEnd) {
            ModifierType = modifierType;

            SubstringStart = substringStart;
            SubstringEnd = substringEnd;
        }
    }
}
