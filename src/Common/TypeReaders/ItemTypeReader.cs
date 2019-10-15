using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common.TypeReaders
{
    public class ItemTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var item = Modules.MarbleBotModule.GetItem<Item>(input);
            if (item == null)
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as an item."));
            }
            else
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(item));
            }
        }
    }
}
