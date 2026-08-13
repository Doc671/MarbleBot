using MarbleBot.Data.Repositories;
using MarbleBot.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace MarbleBot.Modules.Converters
{
    public class UserTypeConverter : TypeConverter<User>
    {
        public override ApplicationCommandOptionType GetDiscordType()
        {
            return ApplicationCommandOptionType.User;
        }

        public override async Task<TypeConverterResult> ReadAsync(IInteractionContext context, IApplicationCommandInteractionDataOption option, IServiceProvider services)
        {
            try
            {
                var discordUser = (IUser)option.Value!;

                var userDao = services.GetRequiredService<UserDao>();

                User marbleBotUser = await userDao.GetOrCreateUserAsync(discordUser.Id, discordUser.Username, discordUser.GlobalName);

                return TypeConverterResult.FromSuccess(marbleBotUser);
            }
            catch (Exception exception)
            {
                return TypeConverterResult.FromError(exception);
            }
        }
    }
}
