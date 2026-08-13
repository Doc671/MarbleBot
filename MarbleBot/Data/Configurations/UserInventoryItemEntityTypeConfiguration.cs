using MarbleBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarbleBot.Data.Configurations;

public class UserInventoryItemEntityTypeConfiguration : IEntityTypeConfiguration<UserInventoryItem>
{
    public void Configure(EntityTypeBuilder<UserInventoryItem> builder)
    {
        builder.HasKey(inventoryItem => new { inventoryItem.UserId, inventoryItem.ItemId });

        builder.HasOne(inventoryItem => inventoryItem.User)
            .WithMany(user => user.Inventory)
            .HasForeignKey(inventoryItem => inventoryItem.UserId);

        builder.HasOne(inventoryItem => inventoryItem.Item)
            .WithMany(user => user.Owners)
            .HasForeignKey(inventoryItem => inventoryItem.ItemId);
    }
}
