using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class ItemBoneNeedle : Item
{
    public Entity storedEntity;
    public const float PIERCE_DAMAGE = 0.5f;
    private const float COOLDOWN_DURATION = 3f;
    private const int NEEDLE_DURABILITY_DAMAGE = 20;
    private double lastUsedTime = -9999;

    public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
    {
        base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
        if (attackedEntity?.Alive == true)
        {
            storedEntity = attackedEntity;
        }
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        IWorldAccessor world = byEntity.World;

        if (byEntity is EntityAgent agent)
        {
            bool needleInRightHand = agent.RightHandItemSlot?.Itemstack?.Collectible == this;
            world.Logger.Debug($"Needle is in {(needleInRightHand ? "right" : "left")} hand");

            ItemSlot offhandSlot = needleInRightHand ? agent.LeftHandItemSlot : agent.RightHandItemSlot;

            if (offhandSlot?.Itemstack == null)
            {
                world.Logger.Debug("Offhand is empty");
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            if (offhandSlot.Itemstack.Collectible is ItemVoodooPoppet poppet)
            {
                if (poppet.storedEntity != null && poppet.storedEntity.Alive)
                {
                    DamageWithNeedle(world, byEntity, poppet, slot, offhandSlot, ref handling);
                    base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                    return;
                }

                if (storedEntity != null && storedEntity.Alive)
                {
                    SetVoodooTarget(world, byEntity, poppet, slot, ref handling);
                    base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                    return;
                }
                else
                {
                    world.Logger.Debug("No stored entity to transfer to poppet.");
                }
            }
        }

        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }

    private void SetVoodooTarget(IWorldAccessor world, EntityAgent byEntity, ItemVoodooPoppet poppet, ItemSlot needleSlot, ref EnumHandHandling handling)
    {
        poppet.storedEntity = storedEntity;

        if (byEntity is IPlayer)
        {
            world.Logger.Debug($"Entity essence captured in voodoo poppet: {storedEntity.Code}");
        }

        needleSlot.TakeOut(1);
        needleSlot.MarkDirty();

        world.PlaySoundAt(new AssetLocation("sounds/break"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);

        handling = EnumHandHandling.PreventDefault;
    }

    private void DamageWithNeedle(IWorldAccessor world, EntityAgent byEntity, ItemVoodooPoppet poppet, ItemSlot needleSlot, ItemSlot voodooSlot, ref EnumHandHandling handling)
    {
        if (world.ElapsedMilliseconds / 1000.0 - lastUsedTime < COOLDOWN_DURATION)
        {
            world.Logger.Debug("Needle is on cooldown.");
            return;
        }

        DamageSource damage = new DamageSource
        {
            Type = EnumDamageType.PiercingAttack,
            SourceEntity = byEntity,
            KnockbackStrength = 0
        };

        ApplyBleedEffect(world, poppet.storedEntity, damage, 5, 1, 0.1f);

        lastUsedTime = world.ElapsedMilliseconds / 1000.0;

        DamageItemSlot(needleSlot, byEntity);
        DamageItemSlot(voodooSlot, byEntity);

        handling = EnumHandHandling.PreventDefault;
    }

    private void DamageItemSlot(ItemSlot itemSlot, EntityAgent byEntity)
    {
        if (itemSlot?.Itemstack != null)
        {
            itemSlot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, itemSlot, NEEDLE_DURABILITY_DAMAGE);
            itemSlot.MarkDirty();
        }
    }

    private void DamageTarget(IWorldAccessor world, Entity target, DamageSource damage, float damageValue)
    {
        if (target == null || !target.Alive)
        {
            world.Logger.Debug("Cannot damage null or dead target");
            return;
        }

        target.ReceiveDamage(damage, damageValue);
        float health = target.WatchedAttributes.GetFloat("health");
        world.Logger.Debug($"Damaged entity: {target.Code}, Health: {health}");
    }

    private void ApplyBleedEffect(IWorldAccessor world, Entity entity, DamageSource damageSource, int duration, int tickInterval, float damageValue)
    {
        if (entity == null || !entity.Alive)
        {
            world.Logger.Debug("Cannot apply bleed effect to null or dead entity");
            return;
        }

        int ticks = duration / tickInterval;
        ApplyBleedTick(world, entity, damageSource, damageValue, ticks, tickInterval);
    }

    private void ApplyBleedTick(IWorldAccessor world, Entity entity, DamageSource damageSource, float damageValue, int remainingTicks, int tickInterval)
    {
        if (!entity.Alive) return;

        SpawnBloodParticles(world, entity);
        DamageTarget(world, entity, damageSource, damageValue);

        if (--remainingTicks > 0)
        {
            world.RegisterCallback(dt => ApplyBleedTick(world, entity, damageSource, damageValue, remainingTicks, tickInterval),
                tickInterval * 1000);
        }
    }

    private void SpawnBloodParticles(IWorldAccessor world, Entity entity)
    {
        Vec3d position = entity.Pos.XYZ.Add(0, entity.CollisionBox.Height / 2, 0);
        world.SpawnParticles(
            new SimpleParticleProperties(
                10,                          // Particle count
                20,                          // Life length in ticks
                ColorUtil.ToRgba(255, 255, 20, 0),
                position.AddCopy(-0.2, 0, -0.2),   // Min position
                position.AddCopy(0.2, 0.5, 0.2),   // Max position
                new Vec3f(-0.05f, 0.1f, -0.05f),   // Min velocity
                new Vec3f(0.05f, 0.2f, 0.05f),     // Max velocity
                1.0f,                        // Min size
                2.0f,                        // Max size
                0.2f,                        // Gravity effect
                1                            // Self propelled
            )
        );
    }
}