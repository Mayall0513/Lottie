using System;
using System.Collections.Generic;

namespace DiscordBot6.Helpers {
    public static class CommandHelper {

        public static bool GetTimeSpan(string[] arguments, out TimeSpan timeSpan, out string[] errors, TimeSpan? minimumTimeSpan = null) {
            List<string> errorsList = new List<string>();

            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;

            for (int i = 0; i < arguments.Length; ++i) {
                string argument = arguments[i];

                if (argument.Length == 1) { // this is too short to be a time
                    errorsList.Add($"Argument `{argument}` is too small to represent a time span");
                    continue;
                }

                if (int.TryParse(argument[0..^1], out int numArgument)) {
                    if (numArgument <= 0) { // a negative time span was given
                        errorsList.Add($"Time Span `{argument}` is negative");
                        continue;
                    }

                    char finalCharacter = argument[^1];
                    switch (finalCharacter) {
                        case 'w':
                            days += numArgument * 7;
                            break;

                        case 'd':
                            days += numArgument;
                            break;

                        case 'h':
                            hours += numArgument;
                            break;

                        case 'm':
                            minutes += numArgument;
                            break;

                        case 's':
                            seconds += numArgument;
                            break;

                        default:
                            errorsList.Add($"Unknown time span suffix `{finalCharacter}`\n**Options:** d, h, m, s");
                            break;
                    }
                }

                else { // value given for time was not a number
                    errorsList.Add($"Time span `{argument}` is not a number");
                }
            }

            timeSpan = new TimeSpan(days, hours, minutes, seconds);

            if (minimumTimeSpan != null && timeSpan < minimumTimeSpan) {
                errorsList.Add($"Time span is too short! Minimum is `{minimumTimeSpan.Value}`");
            }

            errors = errorsList.ToArray();
            return errorsList.Count == 0;
        }
    }
}
