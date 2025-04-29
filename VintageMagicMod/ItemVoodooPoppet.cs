using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

public class ItemVoodooPoppet : Item, IEntityStorer
{
    private double waterStartTime = -1;
    private const float WATER_DROWN_DELAY = 3f; // seconds
    private const int POPPET_DURABILITY_DAMAGE = 50;
    private const float DROWN_DAMAGE = 5f;


    public Entity GetStoredEntity(IWorldAccessor world, ItemStack itemStack) => EntityStorerUtil.GetStoredEntity(world, itemStack);
    public void SetStoredEntity(ItemStack itemStack, Entity entity) => EntityStorerUtil.SetStoredEntity(itemStack, entity);

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        IWorldAccessor world = byEntity.World;
        Entity target = GetStoredEntity(world, slot.Itemstack);

        if (target != null)
        {
            world.Logger.Debug($"Poppet bound to: {target.Code}");
        }
        else
        {
            world.Logger.Debug("Poppet has no stored entity.");
        }

        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        string targetName = itemStack.Attributes.GetString("storedEntityName", "Unknown");
        return Lang.Get("Voodoo Poppet (Target: {0})", targetName);
    }

    public override void OnGroundIdle(EntityItem entityItem)
    {
        base.OnGroundIdle(entityItem);

        IWorldAccessor world = entityItem.World;
        if (world.Side != EnumAppSide.Server) return;

        if (entityItem.Swimming && world.Rand.NextDouble() < 0.01)
        {
            DamageSource damage = new DamageSource
            {
                Type = EnumDamageType.Suffocation,
                KnockbackStrength = 0
            };

            //EntityStorerUtil.DamageItemSlot(slot, byEntity, POPPET_DURABILITY_DAMAGE);
            Entity storedEntity = GetStoredEntity(world, entityItem.Itemstack);
            storedEntity.ReceiveDamage(damage, DROWN_DAMAGE);
        }
    }
}
