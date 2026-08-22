using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class GuildUsableChannelEntityTypeConfiguration : IEntityTypeConfiguration<GuildUsableChannel>
{
    public void Configure(EntityTypeBuilder<GuildUsableChannel> builder)
    {
        builder.HasKey(guildChannel => new { guildChannel.GuildId, guildChannel.ChannelId });

        builder.HasOne(guildChannel => guildChannel.Guild)
            .WithMany(guild => guild.UsableChannels)
            .HasForeignKey(guildChannel => guildChannel.GuildId);
    }
}
