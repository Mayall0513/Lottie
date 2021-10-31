﻿using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static string GetResponseTimeStamp(DateTime dateTime) {
            ulong timeStamp = (ulong) (dateTime - DateTime.UnixEpoch).TotalSeconds;

            return $"<t:{timeStamp}:R>";
        }

        public static int GetRoles(IEnumerable<string> arguments, SocketGuild guild, SocketGuildUser caller, out HashSet<SocketRole> validRoles, out HashSet<SocketRole> lockedRoles, out HashSet<ulong> phantomRoles, out List<string> invalidRoles) {
            validRoles = new HashSet<SocketRole>();
            lockedRoles = new HashSet<SocketRole>();
            phantomRoles = new HashSet<ulong>();
            invalidRoles = new List<string>();

            foreach (string argument in arguments) {
                SocketRole socketRole;

                if (ulong.TryParse(argument, out ulong roleId)) {
                    socketRole = guild.Roles.FirstOrDefault(role => role.Id == roleId);

                    if (socketRole == null) {
                        phantomRoles.Add(roleId);
                        continue;
                    }
                }

                else {
                    string argumentLower = argument.Trim().ToLower();
                    socketRole = guild.Roles.FirstOrDefault(role => role.Name.ToLower() == argumentLower);

                    if (socketRole == null) {
                        invalidRoles.Add(argument);
                        continue;
                    }
                }

                if (socketRole.Position > caller.Hierarchy || socketRole.Position >= guild.CurrentUser.Hierarchy) {
                    lockedRoles.Add(socketRole);
                }

                else {
                    validRoles.Add(socketRole);
                }
            }

            return validRoles.Count + phantomRoles.Count;
        }

        public static string GetUserIdentifier(ulong userId, IUser user = null) {
            if (user == null) {
                return $"`{userId}`";
            }

            else {
                return $"{user.Mention} (`{user.Username}`)";
            }
        }

        public static string GetRoleIdentifier(ulong roleId, IRole role = null) {
            if (role == null) {
                return $"`{roleId}`";
            }

            else {
                return $"{role.Mention} (`{role.Name}`)";
            }
        }

        public static string GetChannelIdentifier(ulong channelId, IChannel channel = null) {
            if (channel == null) {
                return $"`{channelId}`";
            }

            else {
                return $"<#{channel.Id}> (`{channel.Name}`)";
            }
        }
    }
}
