using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot6.Helpers {
    public static class ContextExtensions {
        public static ResponseBuilder CreateResponse(this SocketCommandContext socketCommandContext) {
            return new ResponseBuilder(socketCommandContext);
        }

        public static async Task SendUserNotFoundResponse(this SocketCommandContext socketCommandContext, IUser user) {
            await CreateResponse(socketCommandContext)
                .WithSubject(user.Id, user.GetAvatarUrl(size: 64))
                .WithErrors($"Could not find user with id `{user.Id}`")
                .WithColor(Color.Red)
                .SendMessage();
        }
    }

    public sealed class ResponseBuilder {
        private readonly SocketCommandContext socketCommandContext;
        private readonly EmbedBuilder embedBuilder;

        public ResponseBuilder(SocketCommandContext socketCommandContext) {
            this.socketCommandContext = socketCommandContext;
            embedBuilder = new EmbedBuilder {
                Description = string.Empty
            };

            embedBuilder.WithAuthor(socketCommandContext.Guild.CurrentUser);
        }

        public ResponseBuilder WithSubject(ulong id, string imageUrl = null) {
            embedBuilder.WithFooter(string.Empty + id, imageUrl);
            return this;
        }

        public ResponseBuilder WithText(string text) {
            embedBuilder.Description += text;
            return this;
        }

        public ResponseBuilder WithField(string name, string value, bool inline = false) {
            embedBuilder.AddField(name, value, inline);
            return this;
        }

        public ResponseBuilder WithErrors(params string[] errors) {
            for (int i = 1; i < errors.Length; ++i) {
                embedBuilder.Description += $"**Error #{i}**" + DiscordBot6.DiscordNewLine;
                embedBuilder.Description += errors[i] + DiscordBot6.DiscordNewLine + DiscordBot6.DiscordNewLine;
            }

            return this;
        }

        public ResponseBuilder WithColor(Color color) {
            embedBuilder.WithColor(color);
            return this;
        }

        public async Task SendMessage() {
            Embed embed = embedBuilder.Build();

            await socketCommandContext.Channel.SendMessageAsync(embed: embed);
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

        public static Embed CreateSimpleMixedEmbed(string message) {
            EmbedBuilder embedBuilder = new EmbedBuilder {
                Color = Color.LightOrange
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
