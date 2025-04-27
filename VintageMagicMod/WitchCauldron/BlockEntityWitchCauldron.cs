using System;
using VintageMagicMod.WitchCauldron;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageMagicMod.WitchCauldron
{
    public class BlockEntityWitchCauldron : BlockEntityLiquidContainer
    {
        public int CapacityLitres { get; set; } = 50;

        GuiDialogWitchCauldron invDialog;

        public const int LIQUID_SLOT = 0;
        public const int INPUT_SLOT = 1;

        public const int AUXILIARY_ITEM_SLOT_COUNT = 6;
        public const int TOTAL_SLOT_COUNT = AUXILIARY_ITEM_SLOT_COUNT + 2;

        // Slot 0: Liquid slot
        // Slot 1: Input/Item slot
        // Slot 2,3,4,5,6,7 - auxiliary
        public override string InventoryClassName => "barrel";

        MeshData currentMesh;
        BlockWitchCauldron ownBlock;

        public bool Sealed;
        public double SealedSinceTotalHours;

        public BarrelRecipe CurrentRecipe;
        public int CurrentOutSize;

        public bool CanSeal
        {
            get
            {
                FindMatchingRecipe();
                if (CurrentRecipe != null && CurrentRecipe.SealHours > 0) return true;
                return false;
            }
        }

        public BlockEntityWitchCauldron()
        {
            inventory = new InventoryGeneric(TOTAL_SLOT_COUNT, null, null, (id, self) =>
            {
                if (id == LIQUID_SLOT) return new ItemSlotLiquidOnly(self, 50);
                else if (id == INPUT_SLOT) return new ItemSlotWitchCauldronInput(self);
                else return new ItemSlotWatertight(self, 6);
            });
            inventory.BaseWeight = 1;
            inventory.OnGetSuitability = GetSuitability;


            inventory.SlotModified += Inventory_SlotModified;
            inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed1;
        }

        private static int[] _selectiveSlots;

        public static int[] GetSelectiveSlots()
        {
            if (_selectiveSlots != null)
            {
                return _selectiveSlots;
            }
            else
            {
                _selectiveSlots = new int[AUXILIARY_ITEM_SLOT_COUNT];

                int currentSlotId = 0;

                for (int i = 0; i < AUXILIARY_ITEM_SLOT_COUNT; i++)
                {
                    // Skip over LIQUID_SLOT / INPUT_SLOT, which are not auxiliary
                    while (currentSlotId == LIQUID_SLOT || currentSlotId == INPUT_SLOT)
                    {
                        currentSlotId++;
                    }

                    _selectiveSlots[i] = currentSlotId;

                    currentSlotId++;
                }
            }

            return _selectiveSlots;
        }

        private float Inventory_OnAcquireTransitionSpeed1(EnumTransitionType transType, ItemStack stack, float mul)
        {
            // Don't spoil while sealed
            if (Sealed && CurrentRecipe != null && CurrentRecipe.SealHours > 0) return 0;

            return mul;
        }

        private float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            // prevent for example rot overflowing into the liquid slot, on a shift-click, when slot[INPUT_SLOT] is already full of 64 x rot.   Rot can be accepted in the liquidOnly slot because it has containableProps (perhaps it shouldn't?)
            if (targetSlot == inventory[LIQUID_SLOT])
            {
                if (inventory[INPUT_SLOT].StackSize > 0)
                {
                    ItemStack currentStack = inventory[INPUT_SLOT].Itemstack;
                    ItemStack testStack = sourceSlot.Itemstack;
                    if (currentStack.Collectible.Equals(currentStack, testStack, GlobalConstants.IgnoredStackAttributes)) return -1;
                }
            }

            // normal behavior
            return (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
        }


        protected override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            if (atBlockFace == BlockFacing.UP) return inventory[INPUT_SLOT];
            return null;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            ownBlock = Block as BlockWitchCauldron;

            if (ownBlock?.Attributes?["capacityLitres"].Exists == true)
            {
                CapacityLitres = ownBlock.Attributes["capacityLitres"].AsInt(50);
                (inventory[LIQUID_SLOT] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitres;
            }

            if (api.Side == EnumAppSide.Client && currentMesh == null)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
            }
            if (api.Side == EnumAppSide.Server)
            {
                RegisterGameTickListener(OnEvery3Second, 3000);
            }

            FindMatchingRecipe();
        }

        bool ignoreChange = false;

        private void Inventory_SlotModified(int slotId)
        {
            if (ignoreChange) return;

            if (slotId >= 0 && slotId < TOTAL_SLOT_COUNT)
            {
                invDialog?.UpdateContents();
                if (Api?.Side == EnumAppSide.Client)
                {
                    currentMesh = GenMesh();
                }

                MarkDirty(true);
                FindMatchingRecipe();
            }

        }


        private void FindMatchingRecipe()
        {
            ItemSlot[] inputSlots = new ItemSlot[] { inventory[INPUT_SLOT], inventory[LIQUID_SLOT] };
            CurrentRecipe = null;

            foreach (var recipe in Api.GetBarrelRecipes())
            {
                int outsize;

                if (recipe.Matches(inputSlots, out outsize))
                {
                    ignoreChange = true;

                    if (recipe.SealHours > 0)
                    {
                        CurrentRecipe = recipe;
                        CurrentOutSize = outsize;

                    }
                    else
                    {
                        if (Api?.Side == EnumAppSide.Server)
                        {
                            recipe.TryCraftNow(Api, 0, inputSlots);
                            MarkDirty(true);
                            Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                        }
                    }


                    invDialog?.UpdateContents();
                    if (Api?.Side == EnumAppSide.Client)
                    {
                        currentMesh = GenMesh();
                        MarkDirty(true);
                    }

                    ignoreChange = false;
                    return;
                }
            }
        }


        private void OnEvery3Second(float dt)
        {
            if (!inventory[INPUT_SLOT].Empty && CurrentRecipe == null)
            {
                FindMatchingRecipe();
            }

            if (CurrentRecipe != null)
            {
                if (Sealed && CurrentRecipe.TryCraftNow(Api, Api.World.Calendar.TotalHours - SealedSinceTotalHours, new ItemSlot[] { inventory[INPUT_SLOT], inventory[LIQUID_SLOT] }) == true)
                {
                    MarkDirty(true);
                    Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
                    Sealed = false;
                }

            }
            else
            {
                if (Sealed)
                {
                    Sealed = false;
                    MarkDirty(true);
                }
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            // Deal with situation where the itemStack had some liquid contents, and BEContainer.OnBlockPlaced() placed this into the inputSlot not the liquidSlot
            ItemSlot inputSlot = inventory[INPUT_SLOT];
            ItemSlot liquidSlot = inventory[LIQUID_SLOT];
            if (!inputSlot.Empty && liquidSlot.Empty)
            {
                var liqProps = BlockLiquidContainerBase.GetContainableProps(inputSlot.Itemstack);
                if (liqProps != null)
                {
                    Inventory.TryFlipItems(LIQUID_SLOT, inputSlot);
                }
            }
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (!Sealed)
            {
                base.OnBlockBroken(byPlayer);
            }

            invDialog?.TryClose();
            invDialog = null;
        }


        public void SealBarrel()
        {
            if (Sealed) return;

            Sealed = true;
            SealedSinceTotalHours = Api.World.Calendar.TotalHours;
            MarkDirty(true);
        }




        public void OnPlayerRightClick(IPlayer byPlayer)
        {
            if (Sealed) return;

            FindMatchingRecipe();

            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer);
            }
        }


        protected void toggleInventoryDialogClient(IPlayer byPlayer)
        {
            if (invDialog == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                invDialog = new GuiDialogWitchCauldron(Lang.Get("Cauldron"), Inventory, Pos, Api as ICoreClientAPI);
                invDialog.OnClosed += () =>
                {
                    invDialog = null;
                    capi.Network.SendBlockEntityPacket(Pos, (int)EnumBlockEntityPacketId.Close, null);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };
                invDialog.OpenSound = AssetLocation.Create("sounds/block/barrelopen", "game");
                invDialog.CloseSound = AssetLocation.Create("sounds/block/barrelclose", "game");

                invDialog.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos, (int)EnumBlockEntityPacketId.Open, null);
            }
            else
            {
                invDialog.TryClose();
            }
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);

            if (packetid < 1000)
            {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);

                // Tell server to save this chunk to disk again
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();

                return;
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                player.InventoryManager?.CloseInventory(Inventory);
            }

            if (packetid == (int)EnumBlockEntityPacketId.Open)
            {
                player.InventoryManager?.OpenInventory(Inventory);
            }


            if (packetid == 1337)
            {
                SealBarrel();
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                invDialog?.TryClose();
                invDialog?.Dispose();
                invDialog = null;
            }
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            Sealed = tree.GetBool("sealed");      // Update Sealed status before we generate the new mesh!
            if (Api?.Side == EnumAppSide.Client)
            {
                currentMesh = GenMesh();
                MarkDirty(true);
                invDialog?.UpdateContents();
            }

            SealedSinceTotalHours = tree.GetDouble("sealedSinceTotalHours");

            if (Api != null)
            {
                FindMatchingRecipe();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBool("sealed", Sealed);
            tree.SetDouble("sealedSinceTotalHours", SealedSinceTotalHours);
        }



        internal MeshData GenMesh()
        {
            if (ownBlock == null) return null;

            MeshData mesh = ownBlock.GenMesh(inventory[INPUT_SLOT].Itemstack, inventory[LIQUID_SLOT].Itemstack, Sealed, Pos);

            if (mesh.CustomInts != null)
            {
                for (int i = 0; i < mesh.CustomInts.Count; i++)
                {
                    mesh.CustomInts.Values[i] |= 1 << 27; // Enable weak water wavy
                    mesh.CustomInts.Values[i] |= 1 << 26; // Enabled weak foam
                }
            }

            return mesh;
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            invDialog?.Dispose();
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            mesher.AddMeshData(currentMesh);
            return true;
        }
    }
}
