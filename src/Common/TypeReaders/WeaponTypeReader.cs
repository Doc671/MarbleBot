using Discord.Commands;
using MarbleBot.Common.Games;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common.TypeReaders
{
    public class WeaponTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            var weapon = Item.Find<Weapon>(input);
            return Task.FromResult(TypeReaderResult.FromSuccess(weapon));
        }
    }
}
