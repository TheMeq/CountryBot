using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;

namespace CountryBot.Utilities
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ThrottleCommandAttribute : PreconditionAttribute
    {
        private readonly ThrottleBy _throttleBy;
        private readonly int _limit;
        private readonly int _intervalSeconds;

        public ThrottleCommandAttribute(ThrottleBy throttleBy, int limit, int intervalSeconds)
        {
            _throttleBy = throttleBy;
            _limit = limit;
            _intervalSeconds = intervalSeconds;
        }

        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context,
            ICommandInfo commandInfo, IServiceProvider services)
        {
            var throttleService = services.GetRequiredService<IThrottleService>();
            var command = commandInfo.Name;
            if (throttleService.CheckThrottle(_throttleBy, _limit, _intervalSeconds, context.User, context.Guild, command))
                return PreconditionResult.FromSuccess();
            else
            {
                var reset = throttleService.GetThrottleReset(_throttleBy, _limit, _intervalSeconds, context.User, context.Guild, command);
                await context.Interaction.RespondAsync($"I'm busy right now. Retry in {reset.TotalSeconds:F0} seconds", ephemeral: true);
                return PreconditionResult.FromError("Throttle exceeded");
            }
        }
    }
}