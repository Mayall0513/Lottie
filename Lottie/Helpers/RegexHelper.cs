using Lottie.PhraseRules;
using PCRE;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lottie.Helpers {
    public static class RegexPatterns {
        public const string PATTERN_WORDSTART = "(?:\\s|^)";
        public const string PATTERN_WORDSTART_NOTMESSAGESTART = "(?<!^)(?:\\s)";
        public const string PATTERN_NOT_WORDSTART = "[^\\s]";

        public const string PATTERN_WORDEND = "(?:\\s|$)";
        public const string PATTERN_WORDEND_NOTMESSAGEEND = "(?:\\s)(?!$)";
        public const string PATTERN_NOT_WORDEND = "[^\\s]";

        public const string PATTERN_NOT_BEFORE = "(?!{0})";
        public const string PATTERN_NOT_AFTER = "(?<!{0})";

        public const string PATTERN_ONE_OR_MORE_GROUP = "(?:{0})+";
        public const string PATTERN_EXACT_LENGTH_GROUP = "(?:{0}){{1}}";
        public const string PATTERN_ATLEAST_GROUP = "(?:{0}){{1},}";
        public const string PATTERN_AT_MOST_GROUP = "(?:{0}){,{1}}";

        public const string PATTERN_ONE_OR_MORE_NOGROUP = "{0}+";
        public const string PATTERN_EXACT_LENGTH_NOGROUP = "{0}{{1}}";
        public const string PATTERN_ATLEAST_NOGROUP = "{0}{{1},}";
        public const string PATTERN_AT_MOST_NOGROUP = "{0}{,{1}}";

        public static readonly string[] PATTERNGROUP_WORDSTART = new[] {
            PATTERN_WORDSTART,
            PATTERN_WORDSTART_NOTMESSAGESTART,
            PATTERN_NOT_WORDSTART
        };

        public static readonly string[] PATTERNGROUP_WORDEND = new[] {
            PATTERN_WORDEND,
            PATTERN_WORDEND_NOTMESSAGEEND,
            PATTERN_NOT_WORDEND
        };

        public static readonly string[] PATTERNGROUP_QUANTIFIERS_GROUP = new string[] {
            PATTERN_ONE_OR_MORE_GROUP,
            PATTERN_EXACT_LENGTH_GROUP,
            PATTERN_ATLEAST_GROUP,
            PATTERN_AT_MOST_GROUP
        };

        public static readonly string[] PATTERNGROUP_QUANTIFIERS_NOGROUP = new string[] {
            PATTERN_ONE_OR_MORE_NOGROUP,
            PATTERN_EXACT_LENGTH_NOGROUP,
            PATTERN_ATLEAST_NOGROUP,
            PATTERN_AT_MOST_NOGROUP
        };
    }

    public static class RegexHelper {
        public enum BoundaryFlags : byte {
            NONE,
            REQUIRED,
            BANNED
        }

        public struct RegexToken {
            public string Character { get; set; }
            public int Length { get; set; }
        }

        public static PcreRegex CreateRegex(string text, ulong serverId, PhraseRulePhraseModifiers phraseRulePhraseModifiers) {
            PcreOptions pcreOptions = PcreOptions.Compiled | PcreOptions.Caseless | PcreOptions.Unicode;
            StringBuilder regex = new StringBuilder();

            Stack<PhraseSubstringModifier> remainingSubstringModifiers = new Stack<PhraseSubstringModifier>();
            HashSet<PhraseSubstringModifier> activeSubstringModifiers = new HashSet<PhraseSubstringModifier>();

            Dictionary<string, HashSet<string>> homographCache = new Dictionary<string, HashSet<string>>();
            int characterCountOverride = 0; // 0 for no override, 1 for exact, 2 for minimum, 3 for maximum
            int textIndex = 1;
            int homographModifierCount = 0;

            foreach (RegexToken regexToken in GetTokens(text)) {
                UpdateSubstringModifiers(remainingSubstringModifiers, activeSubstringModifiers, ref homographModifierCount, textIndex);

                if (!homographCache.ContainsKey(regexToken.Character)) {
                    homographCache.Add(regexToken.Character, HomographHelper.GetHomographs(regexToken.Character, serverId, phraseRulePhraseModifiers.HomographOverrides));
                }

                HashSet<string> localHomographs = homographModifierCount > 0 ? new HashSet<string>(homographCache[regexToken.Character]) : homographCache[regexToken.Character];
                foreach (PhraseSubstringModifier substringModifier in activeSubstringModifiers) {
                    switch (substringModifier.ModifierType) {
                        case SubstringModifierType.MODIFIER_CHARACTERCOUNT_EXACT:
                        case SubstringModifierType.MODIFIER_CHARACTERCOUNT_MINIMUM:
                        case SubstringModifierType.MODIFIER_CHARACTERCOUNT_MAXIMUM:
                            characterCountOverride = (int)substringModifier.ModifierType - (int)SubstringModifierType.MODIFIER_CHARACTERCOUNT_EXACT + 1;
                            break;

                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_NO:
                            localHomographs.Clear();
                            break;

                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_ADD:
                            localHomographs.UnionWith(substringModifier.Data);
                            break;

                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_REMOVE:
                            localHomographs.ExceptWith(substringModifier.Data);
                            break;

                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_CUSTOM:
                            localHomographs.Clear();
                            localHomographs.UnionWith(substringModifier.Data);
                            break;
                    }
                }

                StringBuilder homographAggregate = new StringBuilder(); // this contains all homographs that contain multiple characters
                StringBuilder characterAggregate = new StringBuilder(); // this contains all homographs that contain only one character, this is combined into homographAggregate (above) if it's not empty later on
                int characterAggregateSize = 0;

                foreach (string localHomograph in localHomographs) {
                    StringInfo homographInfo = new StringInfo(localHomograph);

                    if (homographInfo.LengthInTextElements == 1) {
                        characterAggregate.Append(localHomograph);
                        characterAggregateSize++;
                    }

                    else {
                        if (homographAggregate.Length > 0) {
                            homographAggregate.Append("|");
                        }

                        homographAggregate.Append("(?:").Append(EscapeString(localHomograph, false)).Append(")");
                    }
                }

                if (characterAggregateSize > 0) { // if the character aggregate isn't empty, we need to combine it with the homograph aggregate
                    if (characterAggregateSize > 1) { // if the character aggregate contains more than 1 character we need to put [...]'s around it
                        characterAggregate.Insert(0, "[").Append("]");
                    }

                    if (homographAggregate.Length > 0) {
                        homographAggregate.Append("|");
                    }

                    homographAggregate.Append(characterAggregate);
                }

                if (characterCountOverride != 1 || regexToken.Length != 1) {
                    string homographs = homographAggregate.ToString();

                    if (homographs.Contains("|")) {
                        regex.AppendFormat(RegexPatterns.PATTERNGROUP_QUANTIFIERS_GROUP[characterCountOverride], homographs, regexToken.Length);
                    }

                    else {
                        regex.AppendFormat(RegexPatterns.PATTERNGROUP_QUANTIFIERS_NOGROUP[characterCountOverride], homographs, regexToken.Length);
                    }
                }

                else {
                    regex.Append(homographAggregate.ToString());
                }

                textIndex += regexToken.Length;
            }

            bool textIsWrapped = false;
            string escapedString;

            BoundaryFlags wordStartFlag = BoundaryFlags.NONE;
            BoundaryFlags wordEndFlag = BoundaryFlags.NONE;
            BoundaryFlags messageStartFlag = BoundaryFlags.NONE;
            BoundaryFlags messageEndFlag = BoundaryFlags.NONE;

            foreach (PhraseRuleModifier phraseRuleModifier in phraseRulePhraseModifiers.Modifiers) {
                switch (phraseRuleModifier.ModifierType) {
                    case PhraseRuleModifierType.MODIFIER_WORD:
                        wordStartFlag = BoundaryFlags.REQUIRED;
                        wordEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_WORDSTART:
                        wordStartFlag = BoundaryFlags.REQUIRED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_WORDEND:
                        wordEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_MESSAGE:
                        messageStartFlag = BoundaryFlags.REQUIRED;
                        messageEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_MESSAGESTART:
                        messageStartFlag = BoundaryFlags.REQUIRED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_MESSAGEEND:
                        messageEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_CASESENSITIVE:
                        pcreOptions &= ~PcreOptions.Caseless;
                        break;

                    case PhraseRuleModifierType.MODIFIER_NOT_WORDSTART:
                        wordStartFlag = BoundaryFlags.BANNED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_NOT_WORDEND:
                        wordEndFlag = BoundaryFlags.BANNED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_NOT_MESSAGESTART:
                        messageStartFlag = BoundaryFlags.BANNED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_NOT_MESSAGEEND:
                        messageEndFlag = BoundaryFlags.BANNED;
                        break;

                    case PhraseRuleModifierType.MODIFIER_NOT_BEFORE:
                    case PhraseRuleModifierType.MODIFIER_NOT_AFTER:
                        if (!textIsWrapped) {
                            regex.Insert(0, "(?:");
                            regex.Append(")");

                            textIsWrapped = true;
                        }

                        foreach (string bannedPhrase in phraseRuleModifier.Data) {
                            escapedString = EscapeString(bannedPhrase, false);

                            if (phraseRuleModifier.ModifierType == PhraseRuleModifierType.MODIFIER_NOT_BEFORE) {
                                regex.AppendFormat(RegexPatterns.PATTERN_NOT_BEFORE, escapedString);
                            }

                            else {
                                regex.Insert(0, string.Format(RegexPatterns.PATTERN_NOT_AFTER, escapedString));
                            }
                        }

                        break;
                }
            }

            AddWordMessageStartRequirement(regex, ref pcreOptions, wordStartFlag, messageStartFlag);
            AddWordMessageEndRequirement(regex, ref pcreOptions, wordEndFlag, messageEndFlag);

            return new PcreRegex(regex.ToString(), pcreOptions);
        }

        public static void UpdateSubstringModifiers(Stack<PhraseSubstringModifier> remainingModifiers, HashSet<PhraseSubstringModifier> activeModifiers, ref int homographCount, int textIndex) {
            int homographCountCopy = homographCount;

            activeModifiers.RemoveWhere(x => {
                if (x.SubstringEnd < textIndex) {
                    switch (x.ModifierType) {
                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_NO:
                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_ADD:
                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_REMOVE:
                        case SubstringModifierType.MODIFIER_HOMOGRAPHS_CUSTOM:
                            homographCountCopy--;
                            break;
                    }

                    return true;
                }

                return false;
            });

            homographCount = homographCountCopy;

            while (remainingModifiers.Count > 0 && remainingModifiers.Peek().SubstringStart <= textIndex) {
                PhraseSubstringModifier substringModifier = remainingModifiers.Pop();

                switch (substringModifier.ModifierType) {
                    case SubstringModifierType.MODIFIER_HOMOGRAPHS_NO:
                    case SubstringModifierType.MODIFIER_HOMOGRAPHS_ADD:
                    case SubstringModifierType.MODIFIER_HOMOGRAPHS_REMOVE:
                    case SubstringModifierType.MODIFIER_HOMOGRAPHS_CUSTOM:
                        homographCount++;
                        break;
                }

                activeModifiers.Add(substringModifier);
            }
        }

        public static RegexToken[] GetTokens(string text) {
            List<RegexToken> homographTokens = new List<RegexToken>(text.Length);
            RegexToken homographToken = new RegexToken();

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

        public static string EscapeString(string text, bool insideCharacterClass = false) {
            if (text.Length == 1) {
                return RequiresEscaping(text[0], insideCharacterClass) ? "\\" + text : text;
            }

            else {
                StringBuilder escapedString = new StringBuilder((int)(text.Length * 1.1));

                foreach (char character in text) {
                    bool requiresEscaping = RequiresEscaping(character, insideCharacterClass);
                    if (requiresEscaping) {
                        escapedString.Append('\\');
                    }

                    escapedString.Append(character);
                }

                return escapedString.ToString();
            }
        }

        public static bool RequiresEscaping(char character, bool insideCharacterClass = false) {
            if (insideCharacterClass) {
                switch (character) {
                    case '^':
                    case '-':
                    case ']':
                    case '\\':
                        return true;

                    default:
                        return false;
                }
            }

            else {
                switch (character) {
                    case '.':
                    case '^':
                    case '$':
                    case '+':
                    case '?':
                    case '(':
                    case ')':
                    case '[':
                    case '{':
                    case '\\':
                    case '|':
                    case '/':
                        return true;

                    default:
                        return false;
                }
            }
        }

        public static void AddWordMessageStartRequirement(StringBuilder textBuilder, ref PcreOptions pcreOptions, BoundaryFlags wordFlag, BoundaryFlags messageFlag) {
            if (wordFlag == BoundaryFlags.REQUIRED && messageFlag == BoundaryFlags.BANNED) {
                textBuilder.Insert(0, RegexPatterns.PATTERNGROUP_WORDSTART[1]);
            }

            else {
                switch (wordFlag) {
                    case BoundaryFlags.REQUIRED:
                        textBuilder.Insert(0, RegexPatterns.PATTERNGROUP_WORDSTART[0]);

                        if (messageFlag == BoundaryFlags.REQUIRED) {
                            pcreOptions |= PcreOptions.Anchored;
                        }

                        break;

                    case BoundaryFlags.BANNED:
                        textBuilder.Insert(0, RegexPatterns.PATTERNGROUP_WORDSTART[2]);
                        break;
                }
            }
        }

        public static void AddWordMessageEndRequirement(StringBuilder textBuilder, ref PcreOptions pcreOptions, BoundaryFlags wordFlag, BoundaryFlags messageFlag) {
            if (wordFlag == BoundaryFlags.REQUIRED && messageFlag == BoundaryFlags.BANNED) {
                textBuilder.Append(RegexPatterns.PATTERNGROUP_WORDEND[1]);
            }

            else {
                switch (wordFlag) {
                    case BoundaryFlags.REQUIRED:
                        textBuilder.Append(RegexPatterns.PATTERNGROUP_WORDEND[0]);

                        if (messageFlag == BoundaryFlags.REQUIRED) {
                            pcreOptions |= PcreOptions.EndAnchored;
                        }

                        break;

                    case BoundaryFlags.BANNED:
                        textBuilder.Append(RegexPatterns.PATTERNGROUP_WORDEND[2]);
                        break;
                }
            }
        }
    }
}
