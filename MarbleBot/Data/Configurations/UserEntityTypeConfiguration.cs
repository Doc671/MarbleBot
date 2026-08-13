using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.HasIndex(user => user.Username);
        builder.Property(user => user.Username).HasMaxLength(32);

        builder.Property(user => user.Balance).HasColumnType("numeric(20,2)");
        builder.Property(user => user.NetWorth).HasColumnType("numeric(20,2)");
    }
}
