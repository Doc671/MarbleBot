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
            MarbleBotUser? closestUser = null;
            // If the input and the username are equal, return straght away
            // Otherwise, find the closest match
            foreach ((_, MarbleBotUser user) in usersDict)
            {
                if (string.Compare(input, user.Name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(user));
                }
                else if (input.Contains(user.Name, StringComparison.OrdinalIgnoreCase)
                    || user.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                {
                    closestUser = user;
                }
            }

            if (closestUser == null)
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                    "Input could not be parsed as a user."));
            }
            else
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(closestUser));
            }
        }
    }
}
