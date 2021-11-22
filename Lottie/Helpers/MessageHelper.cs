using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lottie.Helpers {
    public static class ContextExtensions {
        public static ResponseBuilder CreateResponse(this IMessageChannel channel) {
            return new ResponseBuilder(channel);
        }


        public static async Task SendTimedChannelMuteSuccessAsync(this ResponseBuilder responseBuilder, string message, string channelIdentifier, DateTime start, TimeSpan period) {
            DateTime end = start + period;

            await responseBuilder
                .WithText(message)
                .WithField("Channel", channelIdentifier)
                .WithField("Start", CommandHelper.GetResponseDateTime(start), true)
                .WithField("End", CommandHelper.GetResponseDateTime(end), true)
                .WithColor(Color.Green)
                .SendMessageAsync();
        }

        public static async Task LogTimedChannelMuteSuccessAsync(this ResponseBuilder responseBuilder, string message, string channelIdentifier, DateTime start, TimeSpan period) {
            DateTime end = start + period;

            await responseBuilder
               .WithText(message)
               .WithField("Channel", channelIdentifier)
               .WithField("Start", CommandHelper.GetResponseDateTime(start), true)
               .WithField("End", CommandHelper.GetResponseDateTime(end), true)
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


        public static async Task SendNoPermissionsAsync(this ResponseBuilder responseBuilder) {
            await responseBuilder
                .WithText("Whoops!")
                .WithErrors($"You're not allowed to do that")
                .AsFailure()
                .SendMessageAsync();
        }

        public static async Task SendUserNotFoundAsync(this ResponseBuilder responseBuilder, ulong userId) {
            await responseBuilder
                .WithText($"Could not find user with id `{userId}`")
                .AsFailure()
                .SendMessageAsync();
        }

        public static async Task SendUserNotInVoiceChannelAsync(this ResponseBuilder responseBuilder, IUser user) {
            await responseBuilder
                .WithText("Whoops!")
                .WithErrors($"{user.Mention} is not in a voice channel")
                .AsFailure()
                .SendMessageAsync();
        }

        public static async Task SendInvalidTimeSpanResponseAsync(this ResponseBuilder responseBuilder, string[] timeSpan, params string[] errors) {
            await responseBuilder
                .WithText($"Could not parse time span \n`{string.Join(" ", timeSpan)}`")
                .WithErrors(errors)
                .AsFailure()
                .SendMessageAsync();
        }

        public static async Task SendNoPresetReasonResponseAsync(this ResponseBuilder responseBuilder) {
            await responseBuilder
                .WithText("This command requires a preset reason")
                .AsFailure()
                .SendMessageAsync();
        }

        public static async Task SendInvalidReasonResponseAsync(this ResponseBuilder responseBuilder, string messageType, ulong reasonId) {
            await responseBuilder
                .WithText($"There is no preset message for {messageType} with ID `{reasonId}`")
                .AsFailure()
                .SendMessageAsync();
        }

        public static async Task SendInvalidReasonFormatResponseAsync(this ResponseBuilder responseBuilder, string text) {
            await responseBuilder
                .WithText($"`{text}` is not a valid preset response")
                .AsFailure()
                .SendMessageAsync();
        }
    
        public static async Task SendNoRolesResponseAsync(this ResponseBuilder responseBuilder) {
            await responseBuilder
                .WithText("This command requires one or more roles")
                .AsFailure()
                .SendMessageAsync();
        }
    }

    public sealed class ResponseBuilder {
        private readonly IMessageChannel channel;
        private readonly EmbedBuilder embedBuilder;
        private readonly List<ButtonBuilder> messageButtons;

        public ResponseBuilder(IMessageChannel channel) {
            this.channel = channel;
            embedBuilder = new EmbedBuilder {
                Description = string.Empty
            };

            messageButtons = new List<ButtonBuilder>();
        }

        public ResponseBuilder WithTimeSpan(DateTime start, TimeSpan period) {
            DateTime end = start + period;

            WithField("Start", CommandHelper.GetResponseDateTime(start), true);
            WithField("End", CommandHelper.GetResponseDateTime(end), true);

            return this;
        }

        public ResponseBuilder WithUserSubject(ulong id) {
            embedBuilder.WithFooter(string.Empty + id, null);
            return this;
        }

        public ResponseBuilder WithUserSubject(IUser user) {
            string imageUrl = user.GetAvatarUrl(size: 32) ?? user.GetDefaultAvatarUrl();
            embedBuilder.WithFooter(string.Empty + user.Id, imageUrl);
            return this;
        }

        public ResponseBuilder WithCustomSubject(string subject, string imageUrl = null) {
            embedBuilder.WithFooter(subject, imageUrl);
            return this;
        }

        public ResponseBuilder WithTimeStamp() {
            embedBuilder.WithCurrentTimestamp();
            return this;
        }

        public ResponseBuilder WithText(string text) {
            if (embedBuilder.Description.Length > 0) {
                embedBuilder.Description += Lottie.DiscordNewLine + Lottie.DiscordNewLine;
            }

            embedBuilder.Description += text;
            return this;
        }

        public ResponseBuilder WithField(string name, string value, bool inline = false) {
            if(value != null) {
                embedBuilder.AddField(name, value, inline);
            }
            
            return this;
        }

        public ResponseBuilder WithErrors(params string[] errors) {
            if (embedBuilder.Description.Length > 0) {
                embedBuilder.Description += Lottie.DiscordNewLine + Lottie.DiscordNewLine;
            }

            for (int i = 0; i < errors.Length; ++i) {
                embedBuilder.Description += $"**Error #{i + 1}**" + Lottie.DiscordNewLine;
                embedBuilder.Description += errors[i];

                embedBuilder.Description += Lottie.DiscordNewLine;
                embedBuilder.Description += Lottie.DiscordNewLine;
            }

            return this;
        }

        public ResponseBuilder WithColor(Color color) {
            embedBuilder.WithColor(color);
            return this;
        }

        public ResponseBuilder WithButton(string label, string id, IEmote emote, bool enabled = true) {
            messageButtons.Add(ButtonBuilder.CreateSecondaryButton(label, id, emote).WithDisabled(!enabled));
            return this;
        }

        public ResponseBuilder AsSuccess() {
            return WithColor(Color.Green);
        }

        public ResponseBuilder AsMixed() {
            return WithColor(Color.LightOrange);
        }

        public ResponseBuilder AsFailure() {
            return WithColor(Color.Red);
        }

        public ResponseBuilder AsLog() {
            return WithColor(Color.LighterGrey);
        }

        public Embed GetEmbed() {
            return embedBuilder.Build();
        }

        public MessageComponent GetMessageComponent() {
            ComponentBuilder componentBuilder = new ComponentBuilder();
            foreach (ButtonBuilder button in messageButtons) {
                componentBuilder.WithButton(button);
            }

            return componentBuilder.Build();
        }

        public async Task SendMessageAsync() {
            Embed embed = GetEmbed();

            if (messageButtons.Count > 0) {
                await channel.SendMessageAsync(embed: embed, component: GetMessageComponent());
            }

            else {
                await channel.SendMessageAsync(embed: embed);
            }
            
        }
    }
}
