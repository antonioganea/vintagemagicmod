using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using System;

public interface IEntityStorer
{
    Entity GetStoredEntity(IWorldAccessor world, ItemStack itemStack);
    void SetStoredEntity(ItemStack itemStack, Entity entity);
}

public static class EntityStorerUtil
{
    public static Entity GetStoredEntity(IWorldAccessor world, ItemStack itemStack)
    {
        string entityUidStr = itemStack?.Attributes?.GetString("storedEntityUid", null);
        if (string.IsNullOrEmpty(entityUidStr)) return null;

        if (long.TryParse(entityUidStr, out long entityId))
        {
            Entity entity = world.GetEntityById(entityId);
            if (entity != null && entity.Alive) return entity;
        }

        return null;
    }

    public static void SetStoredEntity(ItemStack itemStack, Entity entity)
    {
        if (entity == null) return;

        itemStack.Attributes.SetString("storedEntityUid", entity.EntityId.ToString());
        itemStack.Attributes.SetString("storedEntityName", entity.GetName() ?? entity.Code?.ToShortString());
    }

    public static void DamageItemSlot(ItemSlot slot, EntityAgent byEntity, int damage)
    {
        if (slot?.Itemstack != null)
        {
            slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot, damage);
            slot.MarkDirty();
        }
    }
}
