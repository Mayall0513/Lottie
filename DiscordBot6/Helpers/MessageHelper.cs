using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot6.Helpers {
    public static class ResponseHelper {
        // issues

        public static async Task SendUserNotFoundResponse(ISocketMessageChannel channel, ulong userId) {
            await channel.SendMessageAsync(embed: MessageHelper.CreateSimpleErrorEmbed($"Could not find user `{userId}`"));
        }

        public static async Task SendUserNotInVoiceChannelResponse(ISocketMessageChannel channel, ulong userId) {
            await channel.SendMessageAsync(embed: MessageHelper.CreateSimpleErrorEmbed($"User `{userId}` is not in a voice channel"));
        }

        public static async Task SendTimeSpanFormatResponse(ISocketMessageChannel channel, IEnumerable<string> errors) {
            await channel.SendMessageAsync(embed: MessageHelper.CreateComplexErrorEmbed("Error(s) In Time Span", errors));
        }

        public static async Task SendNoPermissionsResponse(ISocketMessageChannel channel) {
            await channel.SendMessageAsync(embed: MessageHelper.CreateSimpleErrorEmbed("You're not allowed to do that"));
        }
    }

    public static class MessageHelper {
        public static Embed CreateSimpleSuccessEmbed(string message) {
            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.Green
            };

            embedBuilder.AddField("Message", message, false);
            return embedBuilder.Build();
        }

        public static Embed CreateTimeSpanSimpleSuccessEmbed(string message, DateTime start, TimeSpan timeSpan) {
            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.Green
            };

            embedBuilder.AddField("Message", message, false);
            embedBuilder.AddField("Start", start, true);
            embedBuilder.AddField("End", start + timeSpan, true);

            return embedBuilder.Build();
        }

        public static Embed CreateTimeSpanSimpleInfoEmbed(string message, DateTime start, TimeSpan timeSpan) {
            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.LightGrey,
                Timestamp = DateTime.UtcNow
            };

            embedBuilder.AddField("Message", message, false);
            embedBuilder.AddField("Start", start, true);
            embedBuilder.AddField("End", start + timeSpan, true);

            return embedBuilder.Build();
        }

        public static Embed CreateSimpleErrorEmbed(string message) {
            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.Red
            };

            embedBuilder.AddField("Error", message, false);
            return embedBuilder.Build();
        }

        public static Embed CreateComplexErrorEmbed(string message, IEnumerable<string> errors) {
            EmbedBuilder embedBuilder = new EmbedBuilder {
                Title = message,
                Color = Color.Red
            };

            int index = 1;
            IEnumerator<string> errorsEumerator = errors.GetEnumerator();

            while (errorsEumerator.MoveNext()) {
                embedBuilder.AddField($"#{index}", errorsEumerator.Current, false);
                index++;
            }

            return embedBuilder.Build();
        }
    }
}
