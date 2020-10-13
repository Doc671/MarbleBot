using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common
{
    public class RequireMaximumLengthAttribute : PreconditionAttribute
    {
        private readonly int _maxLength;

        public RequireMaximumLengthAttribute(int maxLength)
        {
            _maxLength = maxLength;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Message.Content.Length > _maxLength)
            {
                return Task.FromResult(PreconditionResult.FromError($"The input message cannot be longer than {_maxLength} characters."));
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
        }
    }
}
