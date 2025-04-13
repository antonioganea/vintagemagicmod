using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent
{
    public class ItemMagicWoodenWand : Item
    {
        public override void OnAttackingWith(IWorldAccessor world, Entity byEntity, Entity attackedEntity, ItemSlot itemslot)
        {
            base.OnAttackingWith(world, byEntity, attackedEntity, itemslot);
            if (attackedEntity.Alive)
            {
                DamageSource fireDamage = new DamageSource()
                {
                    Type = EnumDamageType.Fire,
                    SourceEntity = byEntity,
                    KnockbackStrength = 0
                };
                attackedEntity.ReceiveDamage(fireDamage, 2.0f);
                attackedEntity.Ignite();
                ApplyBleedEffect(world, attackedEntity, fireDamage, duration: 5, tickInterval: 1);
            }
        }

        private void ApplyBleedEffect(IWorldAccessor world, Entity entity, DamageSource damageSource, int duration, int tickInterval)
        {
            int ticks = duration / tickInterval;
            void BleedTick(float dt)
            {
                if (!entity.Alive) return;
                SpawnBloodParticles(world, entity);
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
                    10,  // Min particles
                    20,  // Max particles
                    ColorUtil.ToRgba(255, 255, 20, 0), // Fire color (orange-red, "blood-like" effect)
                    position.AddCopy(-0.2, 0, -0.2), // Min position
                    position.AddCopy(0.2, 0.5, 0.2), // Max position
                    new Vec3f(-0.05f, 0.1f, -0.05f), // Min velocity
                    new Vec3f(0.05f, 0.2f, 0.05f), // Max velocity
                    1.0f,  // Life length min
                    2.0f,  // Life length max
                    0.2f,  // Gravity effect
                    1
                )
            );
        }
    }
}