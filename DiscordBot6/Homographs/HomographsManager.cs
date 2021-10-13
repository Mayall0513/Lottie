using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text;

using DiscordBot6.Phrases;

namespace DiscordBot6.Homographs {
    public struct HomographToken {
        public string Character { get; set; }
        public int Length { get; set; }
    }

    public static class HomographsManager {
        private static Dictionary<string, string[]> homographsTemplate = new Dictionary<string, string[]>();
        private static Dictionary<ulong, Dictionary<string, string[]>> serverTemplateCache = new Dictionary<ulong, Dictionary<string, string[]>>();

        private static readonly string[] characterOverrideSuffixes = new string[] {
            "+",
            "{{0}}",
            "{{0},}",
            "{,{0}}"
        };

        static HomographsManager() {
            homographsTemplate.Add("a", new[] { "🇦", "4"});
            homographsTemplate.Add("b", new[] { "🇧", "6" });
            homographsTemplate.Add("c", new[] { "🇨" });
            homographsTemplate.Add("d", new[] { "🇩", "cl" });
            homographsTemplate.Add("e", new[] { "🇪", "3" });
            homographsTemplate.Add("f", new[] { "🇫" });
            homographsTemplate.Add("g", new[] { "🇬", "6", "9" });
            homographsTemplate.Add("h", new[] { "🇭", "|-|" });
            homographsTemplate.Add("i", new[] { "🇮", "l", "j", "1", "!", "|" });
            homographsTemplate.Add("j", new[] { "🇯", "l", "i", "1", "!", "|" });
            homographsTemplate.Add("l", new[] { "🇱", "j", "i", "1", "!", "|" });
            homographsTemplate.Add("m", new[] { "🇲", "nn", "/\\/\\" });
            homographsTemplate.Add("n", new[] { "🇳", "/\\/" });
            homographsTemplate.Add("o", new[] { "🇴", "0" });
            homographsTemplate.Add("p", new[] { "🇵", "|o" });
            homographsTemplate.Add("q", new[] { "🇶" });
            homographsTemplate.Add("r", new[] { "🇷" });
            homographsTemplate.Add("s", new[] { "🇸", "z", "5", "$" });
            homographsTemplate.Add("t", new[] { "🇹", "7" });
            homographsTemplate.Add("u", new[] { "🇺", "|_|" });
            homographsTemplate.Add("v", new[] { "🇻", "\\/" });
            homographsTemplate.Add("w", new[] { "🇼", "\\/\\/" });
            homographsTemplate.Add("y", new[] { "🇾", "¥" });
            homographsTemplate.Add("z", new[] { "🇿", "s", "5", "$" });

            string[] keys = homographsTemplate.Keys.ToArray();

            foreach (string key in keys) {
                if (!homographsTemplate[key].Contains("key")) {
                    string[] homographs = homographsTemplate[key];

                    Array.Resize(ref homographs, homographs.Length + 1);
                    Array.Copy(homographs, 0, homographs, 1, homographs.Length - 1);

                    homographs[0] = key;
                    homographsTemplate[key] = homographs;
                }
            }
        }

        public static string SubstituteHomographs(ulong serverId, PhraseRule phraseRuleSet) {
            Dictionary<string, string[]> homographCache = new Dictionary<string, string[]>();

            StringBuilder homographs = new StringBuilder();
            HomographToken[] tokens = GetTokens(phraseRuleSet.Text);

            Stack<PhraseSubstringModifier> remainingSubstringModifiers = new Stack<PhraseSubstringModifier>(phraseRuleSet.SubstringModifiers);
            HashSet<PhraseSubstringModifier> activeRules = new HashSet<PhraseSubstringModifier>();

            int textIndex = 1;
            for (int i = 0; i < tokens.Length; ++i) {
                HomographToken token = tokens[i];

                if (!homographCache.ContainsKey(token.Character)) {
                    homographCache.Add(token.Character, GetHomographs(token.Character, serverId, phraseRuleSet.HomographOverrides));
                }

                activeRules.RemoveWhere(activeRule => activeRule.SubstringEnd < textIndex);

                while (remainingSubstringModifiers.Count > 0 && remainingSubstringModifiers.Peek().SubstringStart == textIndex) {
                    activeRules.Add(remainingSubstringModifiers.Pop());
                }

                HashSet<string> localHomographs = new HashSet<string>(homographCache[token.Character]);
                int characterCountOverride = 0; // 0 for no override, 1 for exact, 2 for minimum, 3 for maximum

                foreach (PhraseSubstringModifier substringModifier in activeRules) {
                    switch (substringModifier.ModifierType) {
                        case SubstringModifierType.MODIFIER_CHARACTERCOUNT_EXACT:
                        case SubstringModifierType.MODIFIER_CHARACTERCOUNT_MINIMUM:
                        case SubstringModifierType.MODIFIER_CHARACTERCOUNT_MAXIMUM:
                            characterCountOverride = (int) substringModifier.ModifierType + 1;
                            break;

                        case SubstringModifierType.MODIFIER_NO_HOMOGRAPHS:
                            localHomographs.Clear();
                            break;

                        case SubstringModifierType.MODIFIER_ADD_HOMOGRAPHS:
                            foreach (string newHomograph in substringModifier.Data) {
                                localHomographs.Add(newHomograph);
                            }

                            break;

                        case SubstringModifierType.MODIFIER_REMOVE_HOMOGRAPHS:
                            foreach (string homographToRemove in substringModifier.Data) {
                                localHomographs.Remove(homographToRemove);
                            }
                  
                            break;              

                        case SubstringModifierType.MODIFIER_CUSTOM_HOMOGRAPHS:
                            localHomographs = new HashSet<string>(substringModifier.Data);
                            break;
                    }
                }

                string characterAggregate = string.Empty;
                int aggregateElements = 0;

                List<string> homographElements = new List<string>(localHomographs.Count);

                foreach (string localHomograph in localHomographs) {
                    StringInfo homographInfo = new StringInfo(localHomograph);

                    if (homographInfo.LengthInTextElements > 1) {
                        homographElements.Add($"(?:{PhraseRegexBuilder.EscapeString(localHomograph, false)})");
                    }

                    else {
                        characterAggregate += localHomograph;
                        aggregateElements++;
                    }
                }

                if (aggregateElements > 0) {
                    if (aggregateElements > 1) {
                        homographElements.Add($"[{PhraseRegexBuilder.EscapeString(characterAggregate, true)}]");
                    }

                    else {
                        homographElements.Add(PhraseRegexBuilder.EscapeString(characterAggregate, false));
                    }
                }

                string characterHomographs = string.Join("|", homographElements);

                if (characterCountOverride != 1 || token.Length != 1) {
                    if (homographElements.Count > 1) {
                        characterHomographs = $"(?:{characterHomographs})";
                    }

                    homographs.Append(characterHomographs);
                    homographs.Append(string.Format(characterOverrideSuffixes[characterCountOverride], token.Length));
                }

                else {
                    homographs.Append(characterHomographs);
                }

                // this is much too broad. needs to be an option and/or configurable for which characters appear
                if (i < tokens.Length - 1) {
                    homographs.Append("(?:[[:punct:]]|\\s)*");
                }
                
                textIndex += token.Length;
            }

            return homographs.ToString();
        }

        public static string[] GetHomographs(string character, ulong serverId, IReadOnlyCollection<PhraseHomographOverride> homographOverrides = null) {
            if (!serverTemplateCache.ContainsKey(serverId)) {
                // try get it from database - that isn't set up yet
                serverTemplateCache.Add(serverId, new Dictionary<string, string[]>(homographsTemplate));
            }

            if (homographOverrides != null) {
                Dictionary<string, string[]> serverTemplate = serverTemplateCache[serverId];
                HashSet<string> homographs = new HashSet<string>(serverTemplate.ContainsKey(character) ? serverTemplate[character] : new string[] { character });

                foreach (PhraseHomographOverride homographOverride in homographOverrides) {
                    if (homographOverride.Pattern != character) {
                        continue;
                    }

                    switch (homographOverride.OverrideType) {
                        case HomographOverrideType.OVERRIDE_NO:
                            homographs.Clear();
                            break;

                        case HomographOverrideType.OVERRIDE_ADD:
                            foreach (string newHomograph in homographOverride.Homographs) {     
                                homographs.Add(newHomograph);
                            }

                            break;

                        case HomographOverrideType.OVERRIDE_REMOVE:
                            foreach (string homograph in homographOverride.Homographs) {
                                homographs.Remove(homograph);
                            }
                            
                            break;

                        case HomographOverrideType.OVERRIDE_CUSTOM:
                            homographs = new HashSet<string>(homographOverride.Homographs);
                            break;
                    }
                }

                return homographs.ToArray();
            }

            else {
                if (homographsTemplate.ContainsKey(character)) {
                    return homographsTemplate[character];
                }
                
                else {
                    return new string[] { character };
                }
            }
        }

        public static HomographToken[] GetTokens(string text) {
            List<HomographToken> homographTokens = new List<HomographToken>();
            HomographToken homographToken = new HomographToken();

            string character = null;
            int characterIndex = 0;
            int tokenLength = 1;

            while (characterIndex < text.Length) {
                if (character == null) {
                    character = StringInfo.GetNextTextElement(text, characterIndex);
                }

                else {
                    string nextCharacter = StringInfo.GetNextTextElement(text, characterIndex);

                    if (nextCharacter == character) {
                        tokenLength++;
                    }

                    else {
                        homographToken.Character = character;
                        homographToken.Length = tokenLength;

                        homographTokens.Add(homographToken);

                        character = nextCharacter;
                        tokenLength = 1;
                    }
                }

                characterIndex += character.Length;
            }

            homographToken.Character = character;
            homographToken.Length = tokenLength;
            homographTokens.Add(homographToken);

            return homographTokens.ToArray();
        }
    }
}
