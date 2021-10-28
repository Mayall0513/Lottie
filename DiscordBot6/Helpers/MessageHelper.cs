using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot6.Helpers {
    public static class ContextExtensions {
        public static ResponseBuilder CreateResponse(this IMessageChannel channel) {
            return new ResponseBuilder(channel);
        }



        // Successes
        
        public static async Task SendGenericSuccessResponse(this IMessageChannel channel, ulong subjectId, string subjectAvatar, string message) {
            await CreateResponse(channel)
                .WithSubject(subjectId, subjectAvatar)
                .WithText(message)
                .WithColor(Color.Green)
                .SendMessage();
        }

        public static async Task LogGenericSuccess(this IMessageChannel channel, ulong subjectId, string subjectAvatar, string message) {
            await CreateResponse(channel)
                .WithSubject(subjectId, subjectAvatar)
                .WithText(message)
                .WithColor(Color.LighterGrey)
                .SendMessage();
        }


        public static async Task SendTimeFrameSuccessResponse(this IMessageChannel channel, ulong subjectId, string subjectAvatar, string message, DateTime start, TimeSpan period) {
            await CreateResponse(channel)
                .WithSubject(subjectId, subjectAvatar)
                .WithText(message)
                .WithField("Start", start.ToString(), true)
                .WithField("End", (start + period).ToString(), true)
                .WithColor(Color.Green)
                .SendMessage();
        }

        public static async Task LogTimeFrameSuccessResponse(this IMessageChannel channel, ulong subjectId, string subjectAvatar, string message, DateTime start, TimeSpan period) {
            await CreateResponse(channel)
               .WithSubject(subjectId, subjectAvatar)
               .WithText(message)
               .WithField("Start", start.ToString(), true)
               .WithField("End", (start + period).ToString(), true)
               .WithColor(Color.LighterGrey)
               .SendMessage();
        }


        // Errors

        public static async Task SendGenericErrorResponse(this IMessageChannel channel, ulong subjectId, string subjectAvatar, params string[] errors) {
            await CreateResponse(channel)
                .WithSubject(subjectId, subjectAvatar)
                .WithErrors(errors)
                .WithColor(Color.Red)
                .SendMessage();
        }

        public static async Task SendNoPermissionResponse(this IMessageChannel channel, IUser user) {
            await SendGenericErrorResponse(channel, user.Id, user.GetAvatarUrl(size: 64), $"You're not allowed to do that!");
        }

        public static async Task SendUserNotFoundResponse(this IMessageChannel channel, ulong userId) {
            await SendGenericErrorResponse(channel, userId, null, $"Could not find user with id `{userId}`!");
        }

        public static async Task SendUserNotInVoiceChannelResponse(this IMessageChannel channel, IUser user) {
            await SendGenericErrorResponse(channel, user.Id, user.GetAvatarUrl(size: 64), $"{user.Mention} is not in a voice channel!");
        }



        // Mixed

        public static async Task SendGenericMixedResponse(this IMessageChannel channel, ulong subjectId, string subjectAvatar, string message, params string[] errors) {
            await CreateResponse(channel)
                .WithSubject(subjectId, subjectAvatar)
                .WithText(message)
                .WithErrors(errors)
                .WithColor(Color.LightOrange)
                .SendMessage();
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
                embedBuilder.Description += DiscordBot6.DiscordNewLine;
                embedBuilder.Description += DiscordBot6.DiscordNewLine;
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
                embedBuilder.Description += DiscordBot6.DiscordNewLine;
                embedBuilder.Description += DiscordBot6.DiscordNewLine;
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

        public async Task SendMessage() {
            Embed embed = embedBuilder.Build();

            await channel.SendMessageAsync(embed: embed);
        }
    }
}
