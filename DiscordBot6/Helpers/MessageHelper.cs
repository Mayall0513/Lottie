using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
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
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder() {
                Name = "Message",
                Value = message
            };

            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.Green
            };

            embedBuilder.AddField(fieldBuilder);
            return embedBuilder.Build();
        }

        public static Embed CreateTimeSpanSimpleSuccessEmbed(string message, DateTime start, TimeSpan timeSpan) {
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder() {
                Name = "Message",
                Value = message
            };

            EmbedFieldBuilder startBuilder = new EmbedFieldBuilder() {
                Name = "Start",
                Value = start,
                IsInline = true
            };

            EmbedFieldBuilder endBuilder = new EmbedFieldBuilder() {
                Name = "End",
                Value = (start + timeSpan),
                IsInline = true
            };

            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.Green
            };

            embedBuilder.AddField(fieldBuilder);
            embedBuilder.AddField(startBuilder);
            embedBuilder.AddField(endBuilder);

            return embedBuilder.Build();
        }

        public static Embed CreateTimeSpanSimpleInfoEmbed(string message, DateTime start, TimeSpan timeSpan) {
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder() {
                Name = "Message",
                Value = message
            };

            EmbedFieldBuilder startBuilder = new EmbedFieldBuilder() {
                Name = "Start",
                Value = start,
                IsInline = true
            };

            EmbedFieldBuilder endBuilder = new EmbedFieldBuilder() {
                Name = "End",
                Value = (start + timeSpan),
                IsInline = true
            };

            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.LightGrey,
                Timestamp = DateTime.UtcNow
            };

            embedBuilder.AddField(fieldBuilder);
            embedBuilder.AddField(startBuilder);
            embedBuilder.AddField(endBuilder);

            return embedBuilder.Build();
        }

        public static Embed CreateSimpleErrorEmbed(string message) {
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder() {
                Name = "Error",
                Value = message
            };

            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.Red
            };

            embedBuilder.AddField(fieldBuilder);
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
                EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder() {
                    Name = $"#{index}",
                    Value = errorsEumerator.Current
                };

                embedBuilder.AddField(fieldBuilder);
                index++;
            }

            return embedBuilder.Build();
        }
    }
}
