using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common
{
    /// <summary> Requires the command to be exectued in a channel with slowmode enabled. </summary>
    public sealed class RequireSlowmodeAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider services)
        {
            if ((context.Channel as ITextChannel).SlowModeInterval > 0)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("You must be in a channel where slowmode is enabled to use this command."));
        }
    }
}
