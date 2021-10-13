﻿using System.Text;

using PCRE;

using DiscordBot6.Homographs;

namespace DiscordBot6.Phrases {
    public static class RegexPatterns {
        public const string PATTERN_WORDSTART                 = "(?:\\s|^){0}";
        public const string PATTERN_WORDSTART_NOTMESSAGESTART = "(?<!^)(?:\\s){0}";
        public const string PATTERN_NOT_WORDSTART             = "[^\\s]{0}";

        public const string PATTERN_WORDEND               = "{0}(?:\\s|$)";
        public const string PATTERN_WORDEND_NOTMESSAGEEND = "{0}(?:\\s)(?!$)";
        public const string PATTERN_NOT_WORDEND           = "{0}[^\\s]";

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

        public const string PATTERN_NOT_BEFORE = "{0}(?!{1})";
        public const string PATTERN_NOT_AFTER  = "(?<!{1}){0}";

        public static readonly string[] PATTERNGROUP_LOOKAROUNDS = new[] {
            PATTERN_NOT_BEFORE,
            PATTERN_NOT_AFTER
        };
    }

    public static class PhraseRegexBuilder {
        public enum BoundaryFlags : byte {
            NONE,
            REQUIRED,
            BANNED
        }

        public static PcreRegex ConstructRegex(ulong serverId, PhraseRule phraseRuleSet) {
            PcreOptions pcreOptions = PcreOptions.Compiled | PcreOptions.Caseless | PcreOptions.Unicode;

            string homographs = HomographsManager.SubstituteHomographs(serverId, phraseRuleSet);
            bool textIsWrapped = false;
            string escapedString;

            BoundaryFlags wordStartFlag = BoundaryFlags.NONE;
            BoundaryFlags wordEndFlag = BoundaryFlags.NONE;
            BoundaryFlags messageStartFlag = BoundaryFlags.NONE;
            BoundaryFlags messageEndFlag = BoundaryFlags.NONE;

            foreach (PhraseRuleConstraint phraseRuleModifier in phraseRuleSet.Constraints) {
                switch (phraseRuleModifier.RequirementType) {
                    case RuleRequirementType.MODIFIER_WORD:
                        wordStartFlag = BoundaryFlags.REQUIRED;
                        wordEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case RuleRequirementType.MODIFIER_WORDSTART:
                        wordStartFlag = BoundaryFlags.REQUIRED;
                        break;

                    case RuleRequirementType.MODIFIER_WORDEND:
                        wordEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case RuleRequirementType.MODIFIER_MESSAGE:
                        messageStartFlag = BoundaryFlags.REQUIRED;
                        messageEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case RuleRequirementType.MODIFIER_MESSAGESTART:
                        messageStartFlag = BoundaryFlags.REQUIRED;
                        break;

                    case RuleRequirementType.MODIFIER_MESSAGEEND:
                        messageEndFlag = BoundaryFlags.REQUIRED;
                        break;

                    case RuleRequirementType.MODIFIER_CASESENSITIVE:
                        pcreOptions &= ~PcreOptions.Caseless;
                        break;

                    case RuleRequirementType.MODIFIER_NOT_WORDSTART:
                        wordStartFlag = BoundaryFlags.BANNED;
                        break;

                    case RuleRequirementType.MODIFIER_NOT_WORDEND:
                        wordEndFlag = BoundaryFlags.BANNED;
                        break;

                    case RuleRequirementType.MODIFIER_NOT_MESSAGESTART:
                        messageStartFlag = BoundaryFlags.BANNED;
                        break;

                    case RuleRequirementType.MODIFIER_NOT_MESSAGEEND:
                        messageEndFlag = BoundaryFlags.BANNED;
                        break;

                    case RuleRequirementType.MODIFIER_NOT_BEFORE:
                    case RuleRequirementType.MODIFIER_NOT_AFTER:       
                        if (!textIsWrapped) {
                            homographs = $"(?:{homographs})";
                            textIsWrapped = true;
                        }
                        
                        foreach (string bannedPhrase in phraseRuleModifier.Data) {
                            escapedString = EscapeString(bannedPhrase, false);
                            homographs = string.Format(RegexPatterns.PATTERNGROUP_LOOKAROUNDS[phraseRuleModifier.RequirementType - RuleRequirementType.MODIFIER_NOT_BEFORE], homographs, escapedString);
                        }
                        
                        break;
                }
            }

            AddWordMessageStartRequirement(ref homographs, ref pcreOptions, wordStartFlag, messageStartFlag);
            AddWordMessageEndRequirement(ref homographs, ref pcreOptions, wordEndFlag, messageEndFlag);

            return new PcreRegex(homographs, pcreOptions);
        }

        public static string EscapeString(string text, bool insideCharacterClass = false) {
            if (text.Length == 1) {
                return RequiresEscaping(text[0], insideCharacterClass) ? "\\" + text : text;
            }
            
            else {
                StringBuilder escapedString = new StringBuilder((int) (text.Length * 1.1));

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
                switch(character) {
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

        public static void AddWordMessageStartRequirement(ref string text, ref PcreOptions pcreOptions, BoundaryFlags wordFlag, BoundaryFlags messageFlag) {
            AddWordMessageRequirements(ref text, ref pcreOptions, PcreOptions.Anchored, wordFlag, messageFlag, RegexPatterns.PATTERNGROUP_WORDSTART);
        }

        public static void AddWordMessageEndRequirement(ref string text, ref PcreOptions pcreOptions, BoundaryFlags wordFlag, BoundaryFlags messageFlag) {
            AddWordMessageRequirements(ref text, ref pcreOptions, PcreOptions.EndAnchored, wordFlag, messageFlag, RegexPatterns.PATTERNGROUP_WORDEND);
        }

        private static void AddWordMessageRequirements(ref string text, ref PcreOptions pcreOptions, PcreOptions anchorFlag, BoundaryFlags wordFlag, BoundaryFlags messageFlag, string[] patternGroup) {
            if (wordFlag == BoundaryFlags.REQUIRED && messageFlag == BoundaryFlags.BANNED) {
                text = string.Format(patternGroup[1], text);
            }

            else {
                switch (wordFlag) {
                    case BoundaryFlags.REQUIRED:
                        text = string.Format(patternGroup[0], text);

                        if (messageFlag == BoundaryFlags.REQUIRED) {
                            pcreOptions |= anchorFlag;
                        }

                        break;

                    case BoundaryFlags.BANNED:
                        text = string.Format(patternGroup[2], text);
                        break;
                }
            }
        }
    }
}
