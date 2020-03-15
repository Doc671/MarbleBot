using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common.TypeReaders
{
    public class MarbleBotUserTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (ulong.TryParse(input.TrimStart('<').TrimEnd('>').TrimStart('@'), out ulong id))
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(MarbleBotUser.Find(context, id)));
            }

            var usersDict = MarbleBotUser.GetUsers();
            foreach (var user in usersDict)
            {
                if (string.Compare(input, user.Value.Name, true) == 0
                    || input.Contains(user.Value.Name, StringComparison.OrdinalIgnoreCase)
                    || user.Value.Name.Contains(input, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(MarbleBotUser.FindAsync(context, usersDict, user.Key).Result));
                }
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a user."));
        }
    }
}
