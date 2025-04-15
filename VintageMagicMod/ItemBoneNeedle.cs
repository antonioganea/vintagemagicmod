using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class ItemBoneNeedle : Item
{
    public Entity storedEntity;
    public const float PIERCE_DAMAGE = 0.5f;

    private const float COOLDOWN_DURATION = 3f;
    private double lastUsedTime = -9999;  

    public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
    {
        base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);

        if (attackedEntity.Alive)
        {
            storedEntity = attackedEntity;

            DamageSource pierceDamage = new DamageSource()
            {
                Type = EnumDamageType.PiercingAttack,
                SourceEntity = byEntity,
                KnockbackStrength = 0
            };

            lastUsedTime = world.ElapsedMilliseconds / 1000.0;

            ApplyBleedEffect(world, attackedEntity, pierceDamage, 5, 1, PIERCE_DAMAGE);
        }
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        IWorldAccessor world = byEntity.World;

        // Prevent spam by cooldown
        if (world.ElapsedMilliseconds / 1000.0 - lastUsedTime < COOLDOWN_DURATION)
        {
            world.Logger.Debug("Needle is on cooldown.");
            return;
        }

        if (storedEntity != null && storedEntity.Alive)
        {
            DamageSource damage = new DamageSource()
            {
                Type = EnumDamageType.PiercingAttack,
                SourceEntity = byEntity,
                KnockbackStrength = 0
            };

            ApplyBleedEffect(world, storedEntity, damage, 5, 1, 0.1f);

            lastUsedTime = world.ElapsedMilliseconds / 1000.0;

            handling = EnumHandHandling.PreventDefault;
            return;
        }
        else
        {
            world.Logger.Debug("StoredEntity is null or dead");
        }

        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }

    private void DamageTarget(IWorldAccessor world, Entity target, DamageSource damage, float damageValue)
    {
        target.ReceiveDamage(damage, damageValue);
        float health = target.WatchedAttributes.GetFloat("health");
        world.Logger.Debug($"Damaging entity: {target.Code}, Health: {health}");
    }

    private void ApplyBleedEffect(IWorldAccessor world, Entity entity, DamageSource damageSource, int duration, int tickInterval, float damageValue)
    {
        int ticks = duration / tickInterval;
        void BleedTick(float dt)
        {
            if (!entity.Alive) return;
            SpawnBloodParticles(world, entity);
            DamageTarget(world, entity, damageSource, damageValue);
            if (--ticks > 0)
            {
                world.RegisterCallback((w) => BleedTick(dt), tickInterval * 1000);
            }
        }
        BleedTick(0);
    }

    private void SpawnBloodParticles(IWorldAccessor world, Entity entity)
    {
        Vec3d position = entity.Pos.XYZ.Add(0, entity.CollisionBox.Height / 2, 0);
        world.SpawnParticles(
            new SimpleParticleProperties(
                10,
                20,
                ColorUtil.ToRgba(255, 255, 20, 0),
                position.AddCopy(-0.2, 0, -0.2),
                position.AddCopy(0.2, 0.5, 0.2),
                new Vec3f(-0.05f, 0.1f, -0.05f),
                new Vec3f(0.05f, 0.2f, 0.05f),
                1.0f,
                2.0f,
                0.2f,
                1
            )
        );
    }
}
