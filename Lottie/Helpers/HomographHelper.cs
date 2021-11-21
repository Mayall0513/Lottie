using Lottie.PhraseRules;
using System.Collections.Generic;

namespace Lottie.Helpers {
    public static class HomographHelper {
        private readonly static Dictionary<string, HashSet<string>> homographsTemplate = new Dictionary<string, HashSet<string>>();
        private readonly static Dictionary<ulong, Dictionary<string, HashSet<string>>> serverTemplateCache = new Dictionary<ulong, Dictionary<string, HashSet<string>>>();

        static HomographHelper() { // this needs finishing!
            homographsTemplate.Add("a", new HashSet<string>(new[] { "🇦", "4" }));
            homographsTemplate.Add("b", new HashSet<string>(new[] { "🇧", "6" }));
            homographsTemplate.Add("c", new HashSet<string>(new[] { "🇨" }));
            homographsTemplate.Add("d", new HashSet<string>(new[] { "🇩", "cl" }));
            homographsTemplate.Add("e", new HashSet<string>(new[] { "🇪", "3" }));
            homographsTemplate.Add("f", new HashSet<string>(new[] { "🇫" }));
            homographsTemplate.Add("g", new HashSet<string>(new[] { "🇬", "6", "9" }));
            homographsTemplate.Add("h", new HashSet<string>(new[] { "🇭", "|-|" }));
            homographsTemplate.Add("i", new HashSet<string>(new[] { "🇮", "l", "j", "1", "!", "|" }));
            homographsTemplate.Add("j", new HashSet<string>(new[] { "🇯", "l", "i", "1", "!", "|" }));
            homographsTemplate.Add("l", new HashSet<string>(new[] { "🇱", "j", "i", "1", "!", "|" }));
            homographsTemplate.Add("m", new HashSet<string>(new[] { "🇲", "nn", "/\\/\\" }));
            homographsTemplate.Add("n", new HashSet<string>(new[] { "🇳", "/\\/" }));
            homographsTemplate.Add("o", new HashSet<string>(new[] { "🇴", "0" }));
            homographsTemplate.Add("p", new HashSet<string>(new[] { "🇵", "|o" }));
            homographsTemplate.Add("q", new HashSet<string>(new[] { "🇶" }));
            homographsTemplate.Add("r", new HashSet<string>(new[] { "🇷" }));
            homographsTemplate.Add("s", new HashSet<string>(new[] { "🇸", "z", "5", "$" }));
            homographsTemplate.Add("t", new HashSet<string>(new[] { "🇹", "7" }));
            homographsTemplate.Add("u", new HashSet<string>(new[] { "🇺", "|_|" }));
            homographsTemplate.Add("v", new HashSet<string>(new[] { "🇻", "\\/" }));
            homographsTemplate.Add("w", new HashSet<string>(new[] { "🇼", "\\/\\/" }));
            homographsTemplate.Add("y", new HashSet<string>(new[] { "🇾", "¥" }));
            homographsTemplate.Add("z", new HashSet<string>(new[] { "🇿", "s", "5", "$" }));

            foreach (string key in homographsTemplate.Keys) {
                if (!homographsTemplate[key].Contains(key)) {
                    homographsTemplate[key].Add(key);
                }
            }
        }

        public static HashSet<string> GetHomographs(string character, ulong serverId, IReadOnlyCollection<PhraseHomographOverride> homographOverrides = null) {
            if (!serverTemplateCache.ContainsKey(serverId)) {
                // try get it from database - that isn't set up yet
                serverTemplateCache.Add(serverId, new Dictionary<string, HashSet<string>>(homographsTemplate));
            }

            if (homographOverrides != null) {
                Dictionary<string, HashSet<string>> serverTemplate = serverTemplateCache[serverId];
                HashSet<string> homographs = serverTemplate.ContainsKey(character) ? new HashSet<string>(serverTemplate[character]) : new HashSet<string>(new[] { character });

                foreach (PhraseHomographOverride homographOverride in homographOverrides) {
                    if (homographOverride.Pattern != character) {
                        continue;
                    }

                    switch (homographOverride.OverrideType) {
                        case HomographOverrideType.OVERRIDE_NO:
                            homographs.Clear();
                            break;

                        case HomographOverrideType.OVERRIDE_ADD:
                            homographs.UnionWith(homographOverride.Homographs);
                            break;

                        case HomographOverrideType.OVERRIDE_REMOVE:
                            homographs.ExceptWith(homographOverride.Homographs);
                            break;

                        case HomographOverrideType.OVERRIDE_CUSTOM:
                            homographs.Clear();
                            homographs.UnionWith(homographOverride.Homographs);
                            break;
                    }
                }

                return homographs;
            }

            else {
                if (homographsTemplate.ContainsKey(character)) {
                    return homographsTemplate[character];
                }

                else {
                    return new HashSet<string>(new[] { character });
                }
            }
        }
    }
}
