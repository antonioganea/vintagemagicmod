using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent
{
    // Token: 0x02000356 RID: 854
    public class BlockEntityAntonioCauldron : BlockEntityLiquidContainer
    {
        // Token: 0x17000350 RID: 848
        // (get) Token: 0x06001BFC RID: 7164 RVA: 0x00100D8B File Offset: 0x000FEF8B
        // (set) Token: 0x06001BFD RID: 7165 RVA: 0x00100D93 File Offset: 0x000FEF93
        public int CapacityLitres { get; set; } = 50;

        // Token: 0x17000351 RID: 849
        // (get) Token: 0x06001BFE RID: 7166 RVA: 0x00100D9C File Offset: 0x000FEF9C
        public override string InventoryClassName
        {
            get
            {
                return "barrel";
            }
        }

        // Token: 0x17000352 RID: 850
        // (get) Token: 0x06001BFF RID: 7167 RVA: 0x00100DA3 File Offset: 0x000FEFA3
        public bool CanSeal
        {
            get
            {
                this.FindMatchingRecipe();
                return this.CurrentRecipe != null && this.CurrentRecipe.SealHours > 0.0;
            }
        }

        // Token: 0x06001C00 RID: 7168 RVA: 0x00100DCC File Offset: 0x000FEFCC
        public BlockEntityAntonioCauldron()
        {
            this.inventory = new InventoryGeneric(2, null, null, delegate (int id, InventoryGeneric self)
            {
                if (id == 0)
                {
                    return new ItemSlotBarrelInput(self);
                }
                return new ItemSlotLiquidOnly(self, 50f);
            });
            this.inventory.BaseWeight = 1f;
            this.inventory.OnGetSuitability = new GetSuitabilityDelegate(this.GetSuitability);
            this.inventory.SlotModified += this.Inventory_SlotModified;
            this.inventory.OnAcquireTransitionSpeed += this.Inventory_OnAcquireTransitionSpeed1;
        }

        // Token: 0x06001C01 RID: 7169 RVA: 0x00100E69 File Offset: 0x000FF069
        private float Inventory_OnAcquireTransitionSpeed1(EnumTransitionType transType, ItemStack stack, float mul)
        {
            if (this.Sealed && this.CurrentRecipe != null && this.CurrentRecipe.SealHours > 0.0)
            {
                return 0f;
            }
            return mul;
        }

        // Token: 0x06001C02 RID: 7170 RVA: 0x00100E98 File Offset: 0x000FF098
        private float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == this.inventory[1] && this.inventory[0].StackSize > 0)
            {
                ItemStack currentStack = this.inventory[0].Itemstack;
                ItemStack testStack = sourceSlot.Itemstack;
                if (currentStack.Collectible.Equals(currentStack, testStack, GlobalConstants.IgnoredStackAttributes))
                {
                    return -1f;
                }
            }
            // This line was modified from the original dnSPY code ~ antonio:
            //return (isMerge ? (this.inventory.BaseWeight + 3f) : (this.inventory.BaseWeight + 1f)) + (sourceSlot.Inventory is InventoryBasePlayer);

            return (isMerge ? (this.inventory.BaseWeight + 3f) : (this.inventory.BaseWeight + 1f)) + ((sourceSlot.Inventory is InventoryBasePlayer) ? 1.0f : 0.0f);
        }

        // Token: 0x06001C03 RID: 7171 RVA: 0x00100F32 File Offset: 0x000FF132
        protected override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            if (atBlockFace == BlockFacing.UP)
            {
                return this.inventory[0];
            }
            return null;
        }

        // Token: 0x06001C04 RID: 7172 RVA: 0x00100F4C File Offset: 0x000FF14C
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.ownBlock = base.Block as BlockAntonioCauldron;
            BlockAntonioCauldron blockBarrel = this.ownBlock;
            bool flag;
            if (blockBarrel == null)
            {
                flag = false;
            }
            else
            {
                JsonObject attributes = blockBarrel.Attributes;
                flag = ((attributes != null) ? new bool?(attributes["capacityLitres"].Exists) : null).GetValueOrDefault();
            }
            if (flag)
            {
                this.CapacityLitres = this.ownBlock.Attributes["capacityLitres"].AsInt(50);
                (this.inventory[1] as ItemSlotLiquidOnly).CapacityLitres = (float)this.CapacityLitres;
            }
            if (api.Side == EnumAppSide.Client && this.currentMesh == null)
            {
                this.currentMesh = this.GenMesh();
                this.MarkDirty(true, null);
            }
            if (api.Side == EnumAppSide.Server)
            {
                this.RegisterGameTickListener(new Action<float>(this.OnEvery3Second), 3000, 0);
            }
            this.FindMatchingRecipe();
        }

        // Token: 0x06001C05 RID: 7173 RVA: 0x0010103C File Offset: 0x000FF23C
        private void Inventory_SlotModified(int slotId)
        {
            if (this.ignoreChange)
            {
                return;
            }
            if (slotId == 0 || slotId == 1)
            {
                GuiDialogBarrel guiDialogBarrel = this.invDialog;
                if (guiDialogBarrel != null)
                {
                    guiDialogBarrel.UpdateContents();
                }
                ICoreAPI api = this.Api;
                if (api != null && api.Side == EnumAppSide.Client)
                {
                    this.currentMesh = this.GenMesh();
                }
                this.MarkDirty(true, null);
                this.FindMatchingRecipe();
            }
        }

        // Token: 0x06001C06 RID: 7174 RVA: 0x0010109C File Offset: 0x000FF29C
        private void FindMatchingRecipe()
        {
            ItemSlot[] inputSlots = new ItemSlot[]
            {
                this.inventory[0],
                this.inventory[1]
            };
            this.CurrentRecipe = null;
            foreach (BarrelRecipe recipe in this.Api.GetBarrelRecipes())
            {
                int outsize;
                if (recipe.Matches(inputSlots, out outsize))
                {
                    this.ignoreChange = true;
                    if (recipe.SealHours > 0.0)
                    {
                        this.CurrentRecipe = recipe;
                        this.CurrentOutSize = outsize;
                    }
                    else
                    {
                        ICoreAPI api = this.Api;
                        if (api != null && api.Side == EnumAppSide.Server)
                        {
                            recipe.TryCraftNow(this.Api, 0.0, inputSlots);
                            this.MarkDirty(true, null);
                            this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
                        }
                    }
                    GuiDialogBarrel guiDialogBarrel = this.invDialog;
                    if (guiDialogBarrel != null)
                    {
                        guiDialogBarrel.UpdateContents();
                    }
                    ICoreAPI api2 = this.Api;
                    if (api2 != null && api2.Side == EnumAppSide.Client)
                    {
                        this.currentMesh = this.GenMesh();
                        this.MarkDirty(true, null);
                    }
                    this.ignoreChange = false;
                    break;
                }
            }
        }

        // Token: 0x06001C07 RID: 7175 RVA: 0x001011EC File Offset: 0x000FF3EC
        private void OnEvery3Second(float dt)
        {
            if (!this.inventory[0].Empty && this.CurrentRecipe == null)
            {
                this.FindMatchingRecipe();
            }
            if (this.CurrentRecipe != null)
            {
                if (this.Sealed && this.CurrentRecipe.TryCraftNow(this.Api, this.Api.World.Calendar.TotalHours - this.SealedSinceTotalHours, new ItemSlot[]
                {
                    this.inventory[0],
                    this.inventory[1]
                }))
                {
                    this.MarkDirty(true, null);
                    this.Api.World.BlockAccessor.MarkBlockEntityDirty(this.Pos);
                    this.Sealed = false;
                    return;
                }
            }
            else if (this.Sealed)
            {
                this.Sealed = false;
                this.MarkDirty(true, null);
            }
        }

        // Token: 0x06001C08 RID: 7176 RVA: 0x001012C8 File Offset: 0x000FF4C8
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            ItemSlot inputSlot = this.Inventory[0];
            ItemSlot liquidSlot = this.Inventory[1];
            if (!inputSlot.Empty && liquidSlot.Empty && BlockLiquidContainerBase.GetContainableProps(inputSlot.Itemstack) != null)
            {
                this.Inventory.TryFlipItems(1, inputSlot);
            }
        }

        // Token: 0x06001C09 RID: 7177 RVA: 0x00101321 File Offset: 0x000FF521
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (!this.Sealed)
            {
                base.OnBlockBroken(byPlayer);
            }
            GuiDialogBarrel guiDialogBarrel = this.invDialog;
            if (guiDialogBarrel != null)
            {
                guiDialogBarrel.TryClose();
            }
            this.invDialog = null;
        }

        // Token: 0x06001C0A RID: 7178 RVA: 0x0010134B File Offset: 0x000FF54B
        public void SealBarrel()
        {
            if (this.Sealed)
            {
                return;
            }
            this.Sealed = true;
            this.SealedSinceTotalHours = this.Api.World.Calendar.TotalHours;
            this.MarkDirty(true, null);
        }

        // Token: 0x06001C0B RID: 7179 RVA: 0x00101380 File Offset: 0x000FF580
        public void OnPlayerRightClick(IPlayer byPlayer)
        {
            if (this.Sealed)
            {
                return;
            }
            this.FindMatchingRecipe();
            if (this.Api.Side == EnumAppSide.Client)
            {
                this.toggleInventoryDialogClient(byPlayer);
            }
        }

        // Token: 0x06001C0C RID: 7180 RVA: 0x001013A8 File Offset: 0x000FF5A8
        protected void toggleInventoryDialogClient(IPlayer byPlayer)
        {
            if (this.invDialog == null)
            {
                ICoreClientAPI capi = this.Api as ICoreClientAPI;
                this.invDialog = new GuiDialogBarrel(Lang.Get("Barrel", Array.Empty<object>()), this.Inventory, this.Pos, this.Api as ICoreClientAPI);
                this.invDialog.OnClosed += delegate
                {
                    this.invDialog = null;
                    capi.Network.SendBlockEntityPacket(this.Pos, 1001, null);
                    capi.Network.SendPacketClient(this.Inventory.Close(byPlayer));
                };
                this.invDialog.OpenSound = AssetLocation.Create("sounds/block/barrelopen", "game");
                this.invDialog.CloseSound = AssetLocation.Create("sounds/block/barrelclose", "game");
                this.invDialog.TryOpen();
                capi.Network.SendPacketClient(this.Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(this.Pos, 1000, null);
                return;
            }
            this.invDialog.TryClose();
        }

        // Token: 0x06001C0D RID: 7181 RVA: 0x001014D0 File Offset: 0x000FF6D0
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
            if (packetid < 1000)
            {
                this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                return;
            }
            if (packetid == 1001)
            {
                IPlayerInventoryManager inventoryManager = player.InventoryManager;
                if (inventoryManager != null)
                {
                    inventoryManager.CloseInventory(this.Inventory);
                }
            }
            if (packetid == 1000)
            {
                IPlayerInventoryManager inventoryManager2 = player.InventoryManager;
                if (inventoryManager2 != null)
                {
                    inventoryManager2.OpenInventory(this.Inventory);
                }
            }
            if (packetid == 1337)
            {
                this.SealBarrel();
            }
        }

        // Token: 0x06001C0E RID: 7182 RVA: 0x00101570 File Offset: 0x000FF770
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 1001)
            {
                (this.Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(this.Inventory);
                GuiDialogBarrel guiDialogBarrel = this.invDialog;
                if (guiDialogBarrel != null)
                {
                    guiDialogBarrel.TryClose();
                }
                GuiDialogBarrel guiDialogBarrel2 = this.invDialog;
                if (guiDialogBarrel2 != null)
                {
                    guiDialogBarrel2.Dispose();
                }
                this.invDialog = null;
            }
        }

        // Token: 0x06001C0F RID: 7183 RVA: 0x001015E0 File Offset: 0x000FF7E0
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.Sealed = tree.GetBool("sealed", false);
            ICoreAPI api = this.Api;
            if (api != null && api.Side == EnumAppSide.Client)
            {
                this.currentMesh = this.GenMesh();
                this.MarkDirty(true, null);
                GuiDialogBarrel guiDialogBarrel = this.invDialog;
                if (guiDialogBarrel != null)
                {
                    guiDialogBarrel.UpdateContents();
                }
            }
            this.SealedSinceTotalHours = tree.GetDouble("sealedSinceTotalHours", 0.0);
            if (this.Api != null)
            {
                this.FindMatchingRecipe();
            }
        }

        // Token: 0x06001C10 RID: 7184 RVA: 0x0010166B File Offset: 0x000FF86B
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("sealed", this.Sealed);
            tree.SetDouble("sealedSinceTotalHours", this.SealedSinceTotalHours);
        }

        // Token: 0x06001C11 RID: 7185 RVA: 0x00101698 File Offset: 0x000FF898
        internal MeshData GenMesh()
        {
            if (this.ownBlock == null)
            {
                return null;
            }
            MeshData mesh = this.ownBlock.GenMesh(this.inventory[0].Itemstack, this.inventory[1].Itemstack, this.Sealed, this.Pos);
            if (mesh.CustomInts != null)
            {
                for (int i = 0; i < mesh.CustomInts.Count; i++)
                {
                    mesh.CustomInts.Values[i] |= 134217728;
                    mesh.CustomInts.Values[i] |= 67108864;
                }
            }
            return mesh;
        }

        // Token: 0x06001C12 RID: 7186 RVA: 0x0010173C File Offset: 0x000FF93C
        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            GuiDialogBarrel guiDialogBarrel = this.invDialog;
            if (guiDialogBarrel == null)
            {
                return;
            }
            guiDialogBarrel.Dispose();
        }

        // Token: 0x06001C13 RID: 7187 RVA: 0x00101754 File Offset: 0x000FF954
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(this.currentMesh, 1);
            return true;
        }

        // Token: 0x04000EDB RID: 3803
        private GuiDialogBarrel invDialog;

        // Token: 0x04000EDC RID: 3804
        private MeshData currentMesh;

        // Token: 0x04000EDD RID: 3805
        private BlockAntonioCauldron ownBlock;

        // Token: 0x04000EDE RID: 3806
        public bool Sealed;

        // Token: 0x04000EDF RID: 3807
        public double SealedSinceTotalHours;

        // Token: 0x04000EE0 RID: 3808
        public BarrelRecipe CurrentRecipe;

        // Token: 0x04000EE1 RID: 3809
        public int CurrentOutSize;

        // Token: 0x04000EE2 RID: 3810
        private bool ignoreChange;
    }
}
