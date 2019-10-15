using Discord.Commands;
using MarbleBot.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarbleBot.Common.TypeReaders
{
    public class MarbleBotUserTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (ulong.TryParse(input.TrimStart('<').TrimEnd('>').TrimStart('@'), out ulong id))
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(MarbleBotModule.GetUser(context, id)));
            }

            var usersObj = MarbleBotModule.GetUsersObject();
            var usersDict = usersObj.ToObject<Dictionary<ulong, MarbleBotUser>>();
            foreach (var user in usersDict)
            {
                if (string.Compare(input, user.Value.Name, true) == 0)
                {
                    return Task.FromResult(TypeReaderResult.FromSuccess(MarbleBotModule.GetUserAsync(context, usersObj, user.Key).Result));
                }
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a user."));
        }
    }
}
