using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common.TypeReaders
{
    public class ItemTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var item = Item.Find<Item>(input);
            return Task.FromResult(TypeReaderResult.FromSuccess(item));
        }
    }
}
