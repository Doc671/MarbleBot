using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MarbleBot.Common.TypeReaders
{
    public class WeaponTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var weapon = Item.Find<Weapon>(input);
            if (weapon == null)
            {
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a weapon."));
            }
            else
            {
                return Task.FromResult(TypeReaderResult.FromSuccess(weapon));
            }
        }
    }
}
