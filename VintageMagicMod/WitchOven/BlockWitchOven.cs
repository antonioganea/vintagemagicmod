using System.Collections.Generic;
using VintageMagicMod;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

using VintageMagicMod.Utils;

namespace VintageMagicMod.WitchOven;

public class BlockWitchOven : Block, IIgnitable
{
    private WorldInteraction[] interactions;

    private AdvancedParticleProperties[] particles;

    private Vec3f[] basePos;

    private MeshRef cubeModelRef;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        if (api.Side != EnumAppSide.Client)
        {
            return;
        }

        ICoreClientAPI capi = api as ICoreClientAPI;

        DefineInteractions(capi);

        InitializeParticles();
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection bs)
    {
        if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWitchOven BlockEntityWitchOven)
        {
            return BlockEntityWitchOven.OnPlayerRightClick(byPlayer, bs);
        }

        return base.OnBlockInteractStart(world, byPlayer, bs);
    }

    public MeshRef GetMeshData(ICoreClientAPI api)
    {
        if (cubeModelRef == null)
        {
            cubeModelRef = CreateMeshData(api);
        }

        return cubeModelRef;
    }

    private MeshRef CreateMeshData(ICoreClientAPI api)
    {
        AssetLocation heatShape = AssetLocation.Create("block/metal/witchoven/witchoven-heat", VintageMagicModModSystem.Domain);

        heatShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");

        Shape shape = Vintagestory.API.Common.Shape.TryGet(api, heatShape);

        ITexPositionSource t = new SimpleTextureSource();

        api.Tesselator.TesselateShapeWithJointIds("witchoven", shape, out MeshData modeldata4, t, Vec3f.Zero);

        return api.Render.UploadMesh(modeldata4);
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;

        if (capi == null)
        {
            return;
        }

        cubeModelRef?.Dispose();
    }

    #region Interactions
    private void DefineInteractions(ICoreClientAPI capi)
    {
        if (capi != null)
        {
            interactions = ObjectCacheUtil.GetOrCreate(api, "witchOvenInteractions", delegate
            {
                List<ItemStack> list = new List<ItemStack>();
                List<ItemStack> fuelStacklist = new List<ItemStack>();
                List<ItemStack> list2 = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
                foreach (CollectibleObject collectible in api.World.Collectibles)
                {
                    JsonObject attributes = collectible.Attributes;
                    if (attributes != null && attributes.IsTrue("isClayOvenFuel"))
                    {
                        List<ItemStack> handBookStacks = collectible.GetHandBookStacks(capi);
                        if (handBookStacks != null)
                        {
                            fuelStacklist.AddRange(handBookStacks);
                        }
                    }
                    else
                    {
                        if (collectible.Attributes?["bakingProperties"]?.AsObject<BakingProperties>() == null)
                        {
                            CombustibleProperties combustibleProps = collectible.CombustibleProps;
                            if (combustibleProps == null || combustibleProps.SmeltingType != EnumSmeltType.Bake || collectible.CombustibleProps.SmeltedStack == null || collectible.CombustibleProps.MeltingPoint >= 260)
                            {
                                continue;
                            }
                        }

                        List<ItemStack> handBookStacks2 = collectible.GetHandBookStacks(capi);
                        if (handBookStacks2 != null)
                        {
                            list.AddRange(handBookStacks2);
                        }
                    }
                }

                return new WorldInteraction[3]
                {
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-oven-bakeable",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = list.ToArray(),
                        GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
                        {
                            if (wi.Itemstacks.Length == 0)
                            {
                                return null;
                            }
                            return null; // REMOVE
                            // TODO:
                            //return (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWitchOven BlockEntityWitchOven3)) ? null : BlockEntityWitchOven3.CanAdd(wi.Itemstacks);
                        }
                    },
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-oven-fuel",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = fuelStacklist.ToArray(),
                        // TODO :
                        //GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWitchOven BlockEntityWitchOven2)) ? null : BlockEntityWitchOven2.CanAddAsFuel(fuelStacklist.ToArray())
                    },
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-oven-ignite",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = list2.ToArray(),
                        GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
                        {
                            if (wi.Itemstacks.Length == 0)
                            {
                                return null;
                            }
                            return null; // REMOVE
                            // TODO:
                            //return (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityWitchOven BlockEntityWitchOven) || !BlockEntityWitchOven.CanIgnite()) ? null : wi.Itemstacks;
                        }
                    }
                };
            });
        }
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }
    #endregion

    #region IIgnitable
    EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
    {
        if (!(api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityWitchOven).IsBurning)
        {
            return EnumIgniteState.NotIgnitable;
        }
        if (secondsIgniting <= 2f)
        {
            return EnumIgniteState.Ignitable;
        }
        return EnumIgniteState.IgniteNow;
    }
    public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
    {
        BlockEntityWitchOven bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityWitchOven;
        if (bef == null)
        {
            return EnumIgniteState.NotIgnitable;
        }
        return bef.GetIgnitableState(secondsIgniting);
    }
    public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
    {
        BlockEntityWitchOven bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityWitchOven;
        if (bef != null && !bef.canIgniteFuel)
        {
            bef.canIgniteFuel = true;
            bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
        }
        handling = EnumHandling.PreventDefault;
    }
    #endregion

    #region Particles
    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
    {
        return true;
    }

    public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
    {
        if (manager.BlockAccess.GetBlockEntity(pos) is BlockEntityWitchOven BlockEntityWitchOven && BlockEntityWitchOven.IsBurning)
        {
            // TODO :
            //BlockEntityWitchOven.RenderParticleTick(manager, pos, windAffectednessAtPos, secondsTicking, particles);
        }

        base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
    }

    private void InitializeParticles()
    {
        particles = new AdvancedParticleProperties[16];
        basePos = new Vec3f[particles.Length];
        Cuboidf[] array = new Cuboidf[4]
        {
            new Cuboidf(0.125f, 0f, 0.125f, 0.3125f, 0.5f, 0.875f),
            new Cuboidf(0.7125f, 0f, 0.125f, 0.875f, 0.5f, 0.875f),
            new Cuboidf(0.125f, 0f, 0.125f, 0.875f, 0.5f, 0.3125f),
            new Cuboidf(0.125f, 0f, 0.7125f, 0.875f, 0.5f, 0.875f)
        };
        for (int i = 0; i < 4; i++)
        {
            AdvancedParticleProperties advancedParticleProperties = ParticleProperties[0].Clone();
            Cuboidf cuboidf = array[i];
            basePos[i] = new Vec3f(0f, 0f, 0f);
            advancedParticleProperties.PosOffset[0].avg = cuboidf.MidX;
            advancedParticleProperties.PosOffset[0].var = cuboidf.Width / 2f;
            advancedParticleProperties.PosOffset[1].avg = 0.3f;
            advancedParticleProperties.PosOffset[1].var = 0.05f;
            advancedParticleProperties.PosOffset[2].avg = cuboidf.MidZ;
            advancedParticleProperties.PosOffset[2].var = cuboidf.Length / 2f;
            advancedParticleProperties.Quantity.avg = 0.5f;
            advancedParticleProperties.Quantity.var = 0.2f;
            advancedParticleProperties.LifeLength.avg = 0.8f;
            particles[i] = advancedParticleProperties;
        }

        for (int j = 4; j < 8; j++)
        {
            AdvancedParticleProperties advancedParticleProperties2 = ParticleProperties[1].Clone();
            advancedParticleProperties2.PosOffset[1].avg = 0.06f;
            advancedParticleProperties2.PosOffset[1].var = 0.02f;
            advancedParticleProperties2.Quantity.avg = 0.5f;
            advancedParticleProperties2.Quantity.var = 0.2f;
            advancedParticleProperties2.LifeLength.avg = 0.3f;
            advancedParticleProperties2.VertexFlags = 128;
            particles[j] = advancedParticleProperties2;
        }

        for (int k = 8; k < 12; k++)
        {
            AdvancedParticleProperties advancedParticleProperties3 = ParticleProperties[2].Clone();
            advancedParticleProperties3.PosOffset[1].avg = 0.09f;
            advancedParticleProperties3.PosOffset[1].var = 0.02f;
            advancedParticleProperties3.Quantity.avg = 0.5f;
            advancedParticleProperties3.Quantity.var = 0.2f;
            advancedParticleProperties3.LifeLength.avg = 0.18f;
            advancedParticleProperties3.VertexFlags = 192;
            particles[k] = advancedParticleProperties3;
        }

        for (int l = 12; l < 16; l++)
        {
            AdvancedParticleProperties advancedParticleProperties4 = ParticleProperties[3].Clone();
            advancedParticleProperties4.PosOffset[1].avg = 0.12f;
            advancedParticleProperties4.PosOffset[1].var = 0.03f;
            advancedParticleProperties4.Quantity.avg = 0.2f;
            advancedParticleProperties4.Quantity.var = 0.1f;
            advancedParticleProperties4.LifeLength.avg = 0.12f;
            advancedParticleProperties4.VertexFlags = 255;
            particles[l] = advancedParticleProperties4;
        }
    }
    #endregion
}