using Discord;
using System;
using System.Threading.Tasks;

namespace DiscordBot6.Helpers {
    public static class ContextExtensions {
        public static ResponseBuilder CreateResponse(this IMessageChannel channel) {
            return new ResponseBuilder(channel);
        }


        // Successes
        
        public static async Task SendGenericSuccessAsync(this ResponseBuilder responseBuilder, string message) {
            await responseBuilder
                .WithText(message)
                .WithColor(Color.Green)
                .SendMessageAsync();
        }

        public static async Task LogGenericSuccessAsync(this ResponseBuilder responseBuilder, string message) {
            await responseBuilder
                .WithText(message)
                .WithColor(Color.LighterGrey)
                .SendMessageAsync();
        }


        public static async Task SendTimedChannelMuteSuccessAsync(this ResponseBuilder responseBuilder, string message, string channelIdentifier, DateTime start, TimeSpan period) {
            DateTime end = start + period;

            await responseBuilder
                .WithText(message)
                .WithField("Channel", channelIdentifier)
                .WithField("Start", CommandHelper.GetResponseTimeStamp(start), true)
                .WithField("End", CommandHelper.GetResponseTimeStamp(end), true)
                .WithColor(Color.Green)
                .SendMessageAsync();
        }

        public static async Task LogTimedChannelMuteSuccessAsync(this ResponseBuilder responseBuilder, string message, string channelIdentifier, DateTime start, TimeSpan period) {
            DateTime end = start + period;

            await responseBuilder
               .WithText(message)
               .WithField("Channel", channelIdentifier)
               .WithField("Start", CommandHelper.GetResponseTimeStamp(start), true)
               .WithField("End", CommandHelper.GetResponseTimeStamp(end), true)
               .WithColor(Color.LighterGrey)
               .SendMessageAsync();
        }

        public static async Task SendChannelMuteSuccessAsync(this ResponseBuilder responseBuilder, string message, string channelIdentifier) {
            await responseBuilder
                .WithText(message)
                .WithField("Channel", channelIdentifier)
                .WithColor(Color.Green)
                .SendMessageAsync();
        }

        public static async Task LogChannelMuteSuccessAsync(this ResponseBuilder responseBuilder, string message, string channelIdentifier) {
            await responseBuilder
                .WithText(message)
                .WithField("Channel", channelIdentifier)
                .WithColor(Color.LighterGrey)
                .SendMessageAsync();
        }


        // Errors

        public static async Task SendGenericErrorsAsync(this ResponseBuilder responseBuilder, params string[] errors) {
            await responseBuilder.WithErrors(errors)
                .WithColor(Color.Red)
                .SendMessageAsync();
        }

        public static async Task SendNoPermissionAsync(this ResponseBuilder responseBuilder) {
            await SendGenericErrorsAsync(responseBuilder, $"You're not allowed to do that");
        }

        public static async Task SendUserNotFoundAsync(this ResponseBuilder responseBuilder, ulong userId) {
            await SendGenericErrorsAsync(responseBuilder, $"Could not find user with id `{userId}`");
        }

        public static async Task SendUserNotInVoiceChannelAsync(this ResponseBuilder responseBuilder, IUser user) {
            await SendGenericErrorsAsync(responseBuilder, $"{user.Mention} is not in a voice channel");
        }



        // Mixed

        public static async Task SendGenericMixedAsync(this ResponseBuilder responseBuilder, string message, params string[] errors) {
            await responseBuilder
                .WithText(message)
                .WithErrors(errors)
                .WithColor(Color.LightOrange)
                .SendMessageAsync();
        }
    }

    public sealed class ResponseBuilder {
        private readonly IMessageChannel channel;
        private readonly EmbedBuilder embedBuilder;

        public ResponseBuilder(IMessageChannel channel) {
            this.channel = channel;

            embedBuilder = new EmbedBuilder {
                Description = string.Empty
            };
        }

        public ResponseBuilder WithSubject(ulong id, string imageUrl) {
            embedBuilder.WithFooter(string.Empty + id, imageUrl);
            return this;
        }

        public ResponseBuilder WithText(string text) {
            if (embedBuilder.Description.Length > 0) {
                embedBuilder.Description += DiscordBot6.DiscordNewLine + DiscordBot6.DiscordNewLine;
            }

            embedBuilder.Description += text;
            return this;
        }

        public ResponseBuilder WithField(string name, string value, bool inline = false) {
            embedBuilder.AddField(name, value, inline);
            return this;
        }

        public ResponseBuilder WithErrors(params string[] errors) {
            if (embedBuilder.Description.Length > 0) {
                embedBuilder.Description += DiscordBot6.DiscordNewLine + DiscordBot6.DiscordNewLine;
            }

            for (int i = 0; i < errors.Length; ++i) {
                embedBuilder.Description += $"**Error #{i + 1}**" + DiscordBot6.DiscordNewLine;
                embedBuilder.Description += errors[i];

                embedBuilder.Description += DiscordBot6.DiscordNewLine;
                embedBuilder.Description += DiscordBot6.DiscordNewLine;
            }

            return this;
        }

        public ResponseBuilder WithColor(Color color) {
            embedBuilder.WithColor(color);
            return this;
        }

        public async Task SendMessageAsync() {
            Embed embed = embedBuilder.Build();

            await channel.SendMessageAsync(embed: embed);
        }
    }
}
