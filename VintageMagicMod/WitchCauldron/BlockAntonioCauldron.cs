using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VintageMagicMod;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace VintageMagicMod.WitchCauldron
{
    public class BlockAntonioCauldron : BlockLiquidContainerBase
    {
        public override bool AllowHeldLiquidTransfer
        {
            get
            {
                return false;
            }
        }

        public AssetLocation emptyShape { get; protected set; } = AssetLocation.Create("block/metal/cauldron/cauldron-magic", VintageMagicModModSystem.Domain);
        public AssetLocation sealedShape { get; protected set; } = AssetLocation.Create("block/metal/cauldron/cauldron-magic", VintageMagicModModSystem.Domain);
        public AssetLocation contentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/contents", "game");
        public AssetLocation opaqueLiquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/opaqueliquidcontents", VintageMagicModModSystem.Domain);
        public AssetLocation liquidContentsShape { get; protected set; } = AssetLocation.Create("block/wood/barrel/liquidcontents", VintageMagicModModSystem.Domain);

        public override int GetContainerSlotId(BlockPos pos)
        {
            return 1;
        }

        public override int GetContainerSlotId(ItemStack containerStack)
        {
            return 1;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            object obj;
            Dictionary<string, MultiTextureMeshRef> meshrefs;
            if (capi.ObjectCache.TryGetValue("barrelMeshRefs" + Code, out obj))
            {
                meshrefs = obj as Dictionary<string, MultiTextureMeshRef>;
            }
            else
            {
                // These lines below were modified from dnSPY code (trying to do 2 assignments in one line) ~ antonio
                //meshrefs = (capi.ObjectCache["barrelMeshRefs" + this.Code] = new Dictionary<string, MultiTextureMeshRef>());
                meshrefs = new Dictionary<string, MultiTextureMeshRef>();
                capi.ObjectCache["barrelMeshRefs" + Code] = meshrefs;
            }
            ItemStack[] contentStacks = GetContents(capi.World, itemstack);
            if (contentStacks == null || contentStacks.Length == 0)
            {
                return;
            }
            bool issealed = itemstack.Attributes.GetBool("sealed", false);
            string meshkey = GetBarrelMeshkey(contentStacks[0], contentStacks.Length > 1 ? contentStacks[1] : null);
            MultiTextureMeshRef meshRef;
            if (!meshrefs.TryGetValue(meshkey, out meshRef))
            {
                MeshData meshdata = GenMesh(contentStacks[0], contentStacks.Length > 1 ? contentStacks[1] : null, issealed, null);
                meshRef = meshrefs[meshkey] = capi.Render.UploadMultiTextureMesh(meshdata);
            }
            renderinfo.ModelRef = meshRef;
        }

        public string GetBarrelMeshkey(ItemStack contentStack, ItemStack liquidStack)
        {
            return (contentStack != null ? new int?(contentStack.StackSize) : null).ToString() + "x" + (contentStack != null ? new int?(contentStack.GetHashCode()) : null).ToString() + (liquidStack != null ? new int?(liquidStack.StackSize) : null).ToString() + "x" + (liquidStack != null ? new int?(liquidStack.GetHashCode()) : null).ToString();
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null)
            {
                return;
            }
            object obj;
            if (capi.ObjectCache.TryGetValue("barrelMeshRefs", out obj))
            {
                foreach (KeyValuePair<int, MultiTextureMeshRef> val in obj as Dictionary<int, MultiTextureMeshRef>)
                {
                    val.Value.Dispose();
                }
                capi.ObjectCache.Remove("barrelMeshRefs");
            }
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            bool preventDefault = false;
            foreach (BlockBehavior blockBehavior in BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                blockBehavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventDefault)
                {
                    preventDefault = true;
                }
                if (handled == EnumHandling.PreventSubsequent)
                {
                    return;
                }
            }
            if (preventDefault)
            {
                return;
            }
            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack[] drops = new ItemStack[]
                {
                    new ItemStack(this, 1)
                };
                for (int i = 0; i < drops.Length; i++)
                {
                    world.SpawnItemEntity(drops[i], pos, null);
                }
                world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, 0.0, byPlayer, true, 32f, 1f);
            }
            if (EntityClass != null)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                {
                    entity.OnBlockBroken(null);
                }
            }
            world.BlockAccessor.SetBlock(0, pos);
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
        }

        public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
        {
            return base.TryPutLiquid(pos, liquidStack, desiredLitres);
        }

        public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
        {
            return base.TryPutLiquid(containerStack, liquidStack, desiredLitres);
        }

        public MeshData GenMesh(ItemStack contentStack, ItemStack liquidContentStack, bool issealed, BlockPos forBlockPos = null)
        {
            ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
            Shape shape = Vintagestory.API.Common.Shape.TryGet(coreClientAPI, issealed ? sealedShape : emptyShape);
            MeshData barrelMesh;

            coreClientAPI.Tesselator.TesselateShape(this, shape, out barrelMesh, null, null, null);
            if (!issealed)
            {
                JsonObject jsonObject;
                if (liquidContentStack == null)
                {
                    jsonObject = null;
                }
                else
                {
                    JsonObject itemAttributes = liquidContentStack.ItemAttributes;
                    jsonObject = itemAttributes != null ? itemAttributes["waterTightContainerProps"] : null;
                }
                JsonObject containerProps = jsonObject;
                MeshData meshData;
                if ((meshData = getContentMeshFromAttributes(contentStack, liquidContentStack, forBlockPos)) == null)
                {
                    meshData = getContentMeshLiquids(contentStack, liquidContentStack, forBlockPos, containerProps) ?? getContentMesh(contentStack, forBlockPos, contentsShape);
                }
                MeshData contentMesh = meshData;
                if (contentMesh != null)
                {
                    barrelMesh.AddMeshData(contentMesh);
                }
                if (forBlockPos != null)
                {
                    barrelMesh.CustomInts = new CustomMeshDataPartInt(barrelMesh.FlagsCount);
                    barrelMesh.CustomInts.Values.Fill(67108864);
                    barrelMesh.CustomInts.Count = barrelMesh.FlagsCount;
                    barrelMesh.CustomFloats = new CustomMeshDataPartFloat(barrelMesh.FlagsCount * 2);
                    barrelMesh.CustomFloats.Count = barrelMesh.FlagsCount * 2;
                }
            }
            return barrelMesh;
        }

        private MeshData getContentMeshLiquids(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos, JsonObject containerProps)
        {
            bool isopaque = containerProps != null && containerProps["isopaque"].AsBool(false);
            bool isliquid = containerProps != null && containerProps.Exists;
            if (liquidContentStack != null && (isliquid || contentStack == null))
            {
                AssetLocation shapefilepath = contentsShape;
                if (isliquid)
                {
                    shapefilepath = isopaque ? opaqueLiquidContentsShape : liquidContentsShape;
                }
                return getContentMesh(liquidContentStack, forBlockPos, shapefilepath);
            }
            return null;
        }

        private MeshData getContentMeshFromAttributes(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos)
        {
            if (liquidContentStack != null)
            {
                JsonObject itemAttributes = liquidContentStack.ItemAttributes;
                if ((itemAttributes != null ? new bool?(itemAttributes["inBarrelShape"].Exists) : null).GetValueOrDefault())
                {
                    JsonObject itemAttributes2 = liquidContentStack.ItemAttributes;
                    AssetLocation loc = AssetLocation.Create(itemAttributes2 != null ? itemAttributes2["inBarrelShape"].AsString(null) : null, contentStack.Collectible.Code.Domain).WithPathPrefixOnce("shapes").WithPathAppendixOnce(".json");
                    return getContentMesh(contentStack, forBlockPos, loc);
                }
            }
            return null;
        }

        protected MeshData getContentMesh(ItemStack stack, BlockPos forBlockPos, AssetLocation shapefilepath)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            WaterTightContainableProps props = GetContainableProps(stack);
            ITexPositionSource contentSource;
            float fillHeight;
            if (props != null)
            {
                if (props.Texture == null)
                {
                    return null;
                }
                contentSource = new ContainerTextureSource(capi, stack, props.Texture);
                fillHeight = GameMath.Min(new float[]
                {
                    1f,
                    stack.StackSize / props.ItemsPerLitre / Math.Max(50, props.MaxStackSize)
                }) * 10f / 16f;
            }
            else
            {
                contentSource = BlockBarrel.getContentTexture(capi, stack, out fillHeight);
            }
            if (stack == null || contentSource == null)
            {
                return null;
            }
            Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapefilepath);
            if (shape == null)
            {
                api.Logger.Warning(string.Format("Barrel block '{0}': Content shape {1} not found. Will try to default to another one.", Code, shapefilepath));
                return null;
            }
            MeshData contentMesh;
            capi.Tesselator.TesselateShape("barrel", shape, out contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), props != null ? props.GlowLevel : 0, 0, 0, null, null);
            contentMesh.Translate(0f, fillHeight, 0f);
            if (props != null && props.ClimateColorMap != null)
            {
                int col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, -1, 196, 128, false);
                if (forBlockPos != null)
                {
                    col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, -1, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, false);
                }
                byte[] rgba = ColorUtil.ToBGRABytes(col);
                for (int i = 0; i < contentMesh.Rgba.Length; i++)
                {
                    // This line has been modified from dnSPY code (automatic conversions from int to byte) ~ antonio
                    //contentMesh.Rgba[i] = contentMesh.Rgba[i] * rgba[i % 4] / byte.MaxValue;
                    contentMesh.Rgba[i] = (byte)(contentMesh.Rgba[i] * rgba[i % 4] / byte.MaxValue);
                }
            }
            return contentMesh;
        }

        public static ITexPositionSource getContentTexture(ICoreClientAPI capi, ItemStack stack, out float fillHeight)
        {
            ITexPositionSource contentSource = null;
            fillHeight = 0f;
            JsonObject jsonObject;
            if (stack == null)
            {
                jsonObject = null;
            }
            else
            {
                JsonObject itemAttributes = stack.ItemAttributes;
                jsonObject = itemAttributes != null ? itemAttributes["inContainerTexture"] : null;
            }
            JsonObject obj = jsonObject;
            if (obj != null && obj.Exists)
            {
                contentSource = new ContainerTextureSource(capi, stack, obj.AsObject<CompositeTexture>(null));
                fillHeight = GameMath.Min(new float[]
                {
                    0.75f,
                    0.7f * stack.StackSize / stack.Collectible.MaxStackSize
                });
            }
            else if ((stack != null ? stack.Block : null) != null && (stack.Block.DrawType == EnumDrawType.Cube || stack.Block.Shape.Base.Path.Contains("basic/cube")) && capi.BlockTextureAtlas.GetPosition(stack.Block, "up", true) != null)
            {
                contentSource = new BlockTopTextureSource(capi, stack.Block);
                fillHeight = GameMath.Min(new float[]
                {
                    0.75f,
                    0.7f * stack.StackSize / stack.Collectible.MaxStackSize
                });
            }
            else if (stack != null)
            {
                if (stack.Class == EnumItemClass.Block)
                {
                    if (stack.Block.Textures.Count > 1)
                    {
                        return null;
                    }
                    contentSource = new ContainerTextureSource(capi, stack, stack.Block.Textures.FirstOrDefault().Value);
                }
                else
                {
                    if (stack.Item.Textures.Count > 1)
                    {
                        return null;
                    }
                    contentSource = new ContainerTextureSource(capi, stack, stack.Item.FirstTexture);
                }
                fillHeight = GameMath.Min(new float[]
                {
                    0.75f,
                    0.7f * stack.StackSize / stack.Collectible.MaxStackSize
                });
            }
            return contentSource;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            WorldInteraction[] array = new WorldInteraction[1];
            int num = 0;
            WorldInteraction worldInteraction = new WorldInteraction();
            worldInteraction.ActionLangCode = "heldhelp-place";
            worldInteraction.HotKeyCode = "shift";
            worldInteraction.MouseButton = EnumMouseButton.Right;
            worldInteraction.ShouldApply = (wi, bs, es) => true;
            array[num] = worldInteraction;
            return array;
        }

        // Token: 0x06001C2E RID: 7214 RVA: 0x001020FC File Offset: 0x001002FC
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            if (Attributes != null)
            {
                capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(50);
                emptyShape = AssetLocation.Create(Attributes["emptyShape"].AsString(emptyShape), Code.Domain);
                sealedShape = AssetLocation.Create(Attributes["sealedShape"].AsString(sealedShape), Code.Domain);
                contentsShape = AssetLocation.Create(Attributes["contentsShape"].AsString(contentsShape), Code.Domain);
                opaqueLiquidContentsShape = AssetLocation.Create(Attributes["opaqueLiquidContentsShape"].AsString(opaqueLiquidContentsShape), Code.Domain);
                liquidContentsShape = AssetLocation.Create(Attributes["liquidContentsShape"].AsString(liquidContentsShape), Code.Domain);
            }
            emptyShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            sealedShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            contentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            opaqueLiquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            liquidContentsShape.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
            if (api.Side != EnumAppSide.Client)
            {
                return;
            }
            ICoreClientAPI capi = api as ICoreClientAPI;
            interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", delegate
            {
                List<ItemStack> liquidContainerStacks = new List<ItemStack>();
                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (obj is ILiquidSource || obj is ILiquidSink || obj is BlockWateringCan)
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                        if (stacks != null)
                        {
                            liquidContainerStacks.AddRange(stacks);
                        }
                    }
                }
                ItemStack[] lstacks = liquidContainerStacks.ToArray();
                ItemStack[] linenStack = new ItemStack[]
                {
                    new ItemStack(api.World.GetBlock(new AssetLocation("linen-normal-down")), 1)
                };
                return new WorldInteraction[]
                {
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-bucket-rightclick",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = lstacks,
                        GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection ws)
                        {
                            BlockEntityAntonioCauldron blockEntityBarrel = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityAntonioCauldron;
                            if (blockEntityBarrel == null || blockEntityBarrel.Sealed)
                            {
                                return null;
                            }
                            return lstacks;
                        }
                    },
                    new WorldInteraction
                    {
                        ActionLangCode = "blockhelp-barrel-takecottagecheese",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = linenStack,
                        GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection ws)
                        {
                            BlockEntityAntonioCauldron blockEntityBarrel2 = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityAntonioCauldron;
                            string text;
                            if (blockEntityBarrel2 == null)
                            {
                                text = null;
                            }
                            else
                            {
                                ItemStack itemstack = blockEntityBarrel2.Inventory[1].Itemstack;
                                if (itemstack == null)
                                {
                                    text = null;
                                }
                                else
                                {
                                    Item item = itemstack.Item;
                                    if (item == null)
                                    {
                                        text = null;
                                    }
                                    else
                                    {
                                        AssetLocation code = item.Code;
                                        text = code != null ? code.Path : null;
                                    }
                                }
                            }
                            if (text == "cottagecheeseportion")
                            {
                                return linenStack;
                            }
                            return null;
                        }
                    }
                };
            });
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection blockSel, IPlayer forPlayer)
        {
            BlockEntityAntonioCauldron bebarrel = null;
            if (blockSel.Position != null)
            {
                bebarrel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityAntonioCauldron;
            }
            if (bebarrel != null && bebarrel.Sealed)
            {
                return new WorldInteraction[0];
            }
            return base.GetPlacedBlockInteractionHelp(world, blockSel, forPlayer);
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            BlockEntityAntonioCauldron bebarrel = null;
            if (blockSel.Position != null)
            {
                bebarrel = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityAntonioCauldron;
            }
            if (bebarrel != null && bebarrel.Sealed)
            {
                return true;
            }
            bool handled = base.OnBlockInteractStart(world, byPlayer, blockSel);
            if (!handled && !byPlayer.WorldData.EntityControls.ShiftKey && blockSel.Position != null)
            {
                if (bebarrel != null)
                {
                    bebarrel.OnPlayerRightClick(byPlayer);
                }
                return true;
            }
            return handled;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            ItemStack[] contentStacks = GetContents(world, inSlot.Itemstack);
            if (contentStacks != null && contentStacks.Length != 0)
            {
                ItemStack itemstack = contentStacks[0] == null ? contentStacks[1] : contentStacks[0];
                if (itemstack != null)
                {
                    dsc.Append(", " + Lang.Get("{0}x {1}", new object[]
                    {
                        itemstack.StackSize,
                        itemstack.GetName()
                    }));
                }
            }
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            string text = base.GetPlacedBlockInfo(world, pos, forPlayer);
            string aftertext = "";
            int i = text.IndexOfOrdinal(Environment.NewLine + Environment.NewLine);
            if (i > 0)
            {
                aftertext = text.Substring(i);
                text = text.Substring(0, i);
            }
            if (GetCurrentLitres(pos) <= 0f)
            {
                text = "";
            }
            BlockEntityAntonioCauldron bebarrel = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityAntonioCauldron;
            if (bebarrel != null)
            {
                ItemSlot slot = bebarrel.Inventory[0];
                if (!slot.Empty)
                {
                    if (text.Length > 0)
                    {
                        text += " ";
                    }
                    else
                    {
                        text = text + Lang.Get("Contents:", Array.Empty<object>()) + "\n ";
                    }
                    text += Lang.Get("{0}x {1}", new object[]
                    {
                        slot.Itemstack.StackSize,
                        slot.Itemstack.GetName()
                    });
                    text += PerishableInfoCompact(api, slot, 0f, false);
                }
                if (bebarrel.Sealed && bebarrel.CurrentRecipe != null)
                {
                    double hoursPassed = world.Calendar.TotalHours - bebarrel.SealedSinceTotalHours;
                    if (hoursPassed < 3.0)
                    {
                        hoursPassed = Math.Max(0.0, hoursPassed + 0.2);
                    }
                    string timePassedText = hoursPassed > 24.0 ? Lang.Get("{0} days", new object[] { Math.Floor(hoursPassed / (double)api.World.Calendar.HoursPerDay * 10.0) / 10.0 }) : Lang.Get("{0} hours", new object[] { Math.Floor(hoursPassed) });
                    string timeTotalText = bebarrel.CurrentRecipe.SealHours > 24.0 ? Lang.Get("{0} days", new object[] { Math.Round(bebarrel.CurrentRecipe.SealHours / (double)api.World.Calendar.HoursPerDay, 1) }) : Lang.Get("{0} hours", new object[] { Math.Round(bebarrel.CurrentRecipe.SealHours) });
                    text = text + "\n" + Lang.Get("Sealed for {0} / {1}", new object[] { timePassedText, timeTotalText });
                }
            }
            return text + aftertext;
        }

        public override void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
        {
        }
    }
}
