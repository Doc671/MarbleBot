using MarbleBot.Entities.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations.Games;

internal class SignupGroupEntryEntityTypeConfiguration : IEntityTypeConfiguration<SignupEntry>
{
    public void Configure(EntityTypeBuilder<SignupEntry> builder)
    {
        builder.HasOne(entry => entry.SignupGroup)
            .WithMany(signupGroup => signupGroup.Entries)
            .HasForeignKey(entry => entry.SignupGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entry => entry.User)
            .WithMany()
            .HasForeignKey(entry => entry.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
