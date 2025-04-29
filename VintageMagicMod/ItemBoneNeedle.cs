using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;

public class ItemBoneNeedle : Item, IEntityStorer
{
    private const float COOLDOWN_DURATION = 3f;
    private const int NEEDLE_DURABILITY_DAMAGE = 20;
    private const float PIERCE_DAMAGE = 0.5f;

    private double lastUsedTime = -9999;

    public Entity GetStoredEntity(IWorldAccessor world, ItemStack itemStack) => EntityStorerUtil.GetStoredEntity(world, itemStack);
    public void SetStoredEntity(ItemStack itemStack, Entity entity) => EntityStorerUtil.SetStoredEntity(itemStack, entity);

    public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemSlot)
    {
        base.OnAttackingWith(world, byEntity, attackedEntity, itemSlot);

        if (itemSlot?.Itemstack == null || attackedEntity?.Alive != true) return;

        if (GetStoredEntity(world, itemSlot.Itemstack) == null)
        {
            SetStoredEntity(itemSlot.Itemstack, attackedEntity);
            itemSlot.MarkDirty();
            world.Logger.Debug("Captured entity into bone needle.");
        }
    }

    public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
    {
        IWorldAccessor world = byEntity.World;

        world.Logger.Debug("OnHeldInteractStop.");
        bool needleInRightHand = (byEntity as EntityAgent)?.RightHandItemSlot?.Itemstack?.Collectible == this;
        ItemSlot offhandSlot = needleInRightHand ? byEntity.LeftHandItemSlot : byEntity.RightHandItemSlot;

        if (offhandSlot?.Itemstack?.Collectible is ItemVoodooPoppet poppet)
        {
        world.Logger.Debug("poppet in right hand.");
            if (poppet.GetStoredEntity(world, offhandSlot.Itemstack) != null)
            {
                DamageWithNeedle(world, byEntity, poppet, slot, offhandSlot);
            }
            else
            {
                Entity stored = GetStoredEntity(world, slot.Itemstack);
                if (stored != null)
                {
                    SetStoredEntity(offhandSlot.Itemstack, stored);
                    slot.TakeOut(1);
                    slot.MarkDirty();
                    world.PlaySoundAt(new AssetLocation("sounds/break"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
                }
                else
                {
                    StoreSelf(world, slot, byEntity);
                }
            }
        }
        else
        {
            StoreSelf(world, slot, byEntity);
        }

        base.OnHeldInteractStop(secondsUsed,slot, byEntity, blockSel, entitySel);
    }

    private void StoreSelf(IWorldAccessor world, ItemSlot slot, EntityAgent byEntity)
    {
        if (GetStoredEntity(world, slot.Itemstack) == null)
        {
            SetStoredEntity(slot.Itemstack, byEntity);
            slot.MarkDirty();
            world.PlaySoundAt(new AssetLocation("sounds/player/stab"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
            world.Logger.Debug("Set self as stored entity in bone needle.");
        }
        else
        {
            world.Logger.Debug("Needle already has a stored entity.");
        }
    }

    private void DamageWithNeedle(IWorldAccessor world, EntityAgent byEntity, ItemVoodooPoppet poppet, ItemSlot needleSlot, ItemSlot poppetSlot)
    {
        if (world.ElapsedMilliseconds / 1000.0 - lastUsedTime < COOLDOWN_DURATION)
        {
            world.Logger.Debug("Needle cooldown active.");
            return;
        }

        Entity target = poppet.GetStoredEntity(world, poppetSlot.Itemstack);
        if (target == null || !target.Alive)
        {
            world.Logger.Debug("Voodoo target not available.");
            return;
        }

        DamageSource damageSource = new DamageSource
        {
            Type = EnumDamageType.PiercingAttack,
            SourceEntity = byEntity,
            KnockbackStrength = 0
        };

        ApplyBleedEffect(world, target, damageSource, 5, 1, 0.1f);

        EntityStorerUtil.DamageItemSlot(needleSlot, byEntity, NEEDLE_DURABILITY_DAMAGE);
        EntityStorerUtil.DamageItemSlot(poppetSlot, byEntity, NEEDLE_DURABILITY_DAMAGE);

        lastUsedTime = world.ElapsedMilliseconds / 1000.0;
    }

    private void ApplyBleedEffect(IWorldAccessor world, Entity entity, DamageSource source, int duration, int interval, float damagePerTick)
    {
        if (!entity.Alive) return;

        int ticks = duration / interval;
        ApplyBleedTick(world, entity, source, damagePerTick, ticks, interval);
    }

    private void ApplyBleedTick(IWorldAccessor world, Entity entity, DamageSource source, float damagePerTick, int remainingTicks, int interval)
    {
        if (!entity.Alive) return;

        SpawnBloodParticles(world, entity);
        entity.ReceiveDamage(source, damagePerTick);

        if (--remainingTicks > 0)
        {
            world.RegisterCallback(dt => ApplyBleedTick(world, entity, source, damagePerTick, remainingTicks, interval), interval * 1000);
        }
    }

    private void SpawnBloodParticles(IWorldAccessor world, Entity entity)
    {
        Vec3d pos = entity.Pos.XYZ.Add(0, entity.CollisionBox.Height / 2, 0);
        world.SpawnParticles(new SimpleParticleProperties(
            10, 20, ColorUtil.ToRgba(255, 255, 20, 0),
            pos.AddCopy(-0.2, 0, -0.2),
            pos.AddCopy(0.2, 0.5, 0.2),
            new Vec3f(-0.05f, 0.1f, -0.05f),
            new Vec3f(0.05f, 0.2f, 0.05f),
            1f, 2f, 0.2f, 1
        ));
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        string targetName = itemStack.Attributes.GetString("storedEntityName", "Unknown");
        return Lang.Get("Bone Needle (Target: {0})", targetName);
    }
}
