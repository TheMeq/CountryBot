using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace CountryBot.Utilities
{
    public enum ThrottleBy
    {
        User,
        Guild
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ThrottleAttribute : PreconditionAttribute
    {
        private readonly ThrottleBy _throttleBy;
        private readonly int _limit;
        private readonly int _intervalSeconds;

        public ThrottleAttribute(ThrottleBy throttleBy, int limit, int intervalSeconds)
        {
            _throttleBy = throttleBy;
            _limit = limit;
            _intervalSeconds = intervalSeconds;
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
            ICommandInfo commandInfo, IServiceProvider services)
        {
            var throttleService = services.GetRequiredService<IThrottleService>();

            if (throttleService.CheckThrottle(_throttleBy, _limit, _intervalSeconds, context.User, context.Guild))
                return PreconditionResult.FromSuccess();
            else
            {
                var reset = throttleService.GetThrottleReset(_throttleBy, _limit, _intervalSeconds, context.User, context.Guild);
                await context.Interaction.RespondAsync($"This command is throttled to prevent rate limiting. You can run this command in {reset.Minutes} minutes.", ephemeral: true);
                return PreconditionResult.FromError("Throttle exceeded");
            }
        }
    }
}