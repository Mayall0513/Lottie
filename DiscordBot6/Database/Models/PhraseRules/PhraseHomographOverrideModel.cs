﻿using DiscordBot6.PhraseRules;
using System.Collections.Generic;

namespace DiscordBot6.Database.Models.PhraseRules {
    public sealed class PhraseHomographOverrideModel : IModelFor<PhraseHomographOverride> {
        public ulong Id { get; set; }
        public int OverrideType { get; set; }
        public string Pattern { get; set; }

        public List<string> Homographs { get; set; } = new List<string>();

        public PhraseHomographOverride CreateConcrete() {
            return new PhraseHomographOverride((HomographOverrideType)OverrideType, Pattern, Homographs);
        }
    }
}
