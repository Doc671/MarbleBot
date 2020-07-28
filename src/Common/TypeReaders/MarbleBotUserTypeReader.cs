using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common.TypeReaders
{
    public class MarbleBotUserTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            if (ulong.TryParse(input.TrimStart('<').TrimEnd('>').TrimStart('@'), out ulong id))
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(MarbleBotUser.Find(context, id)));
            }

            var usersDict = MarbleBotUser.GetUsers();
            foreach ((ulong userId, MarbleBotUser user) in usersDict)
            {
                if (string.Compare(input, user.Name, StringComparison.OrdinalIgnoreCase) == 0
                    || input.Contains(user.Name, StringComparison.OrdinalIgnoreCase)
                    || user.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(MarbleBotUser.FindAsync(context, usersDict, userId).Result));
                }
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                "Input could not be parsed as a user."));
        }
    }
}
