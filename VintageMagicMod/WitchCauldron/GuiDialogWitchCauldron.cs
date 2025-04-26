using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace VintageMagicMod.WitchCauldron;

public class GuiDialogWitchCauldron : GuiDialogBlockEntity
{
    private EnumPosFlag screenPos;

    private ElementBounds inputSlotBounds;

    protected override double FloatyDialogPosition => 0.6;

    protected override double FloatyDialogAlign => 0.8;

    public override double DrawOrder => 0.2;

    public GuiDialogWitchCauldron(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi)
        : base(dialogTitle, inventory, blockEntityPos, capi)
    {
        _ = IsDuplicate;
    }

    private void SetupDialog()
    {
        ElementBounds barrelBoundsLeft = ElementBounds.Fixed(0.0, 30.0, 150.0, 200.0);
        ElementBounds barrelBoundsRight = ElementBounds.Fixed(170.0, 30.0, 150.0, 200.0);
        inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 30.0, 1, 1);
        inputSlotBounds.fixedHeight += 10.0;
        _ = inputSlotBounds.fixedHeight;
        _ = inputSlotBounds.fixedY;
        ElementBounds fullnessMeterBounds = ElementBounds.Fixed(100.0, 30.0, 40.0, 200.0);
        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(barrelBoundsLeft, barrelBoundsRight);
        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithFixedAlignmentOffset(IsRight(screenPos) ? 0.0 - GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0.0).WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);
        SingleComposer = capi.Gui.CreateCompo("blockentityantoniocauldron" + BlockEntityPosition, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
            .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[1], inputSlotBounds, "inputSlot")
            .AddSmallButton(Lang.Get("barrel-seal"), onSealClick, ElementBounds.Fixed(0.0, 100.0, 80.0, 25.0))
            .AddInset(fullnessMeterBounds.ForkBoundingParent(2.0, 2.0, 2.0, 2.0), 2)
            .AddDynamicCustomDraw(fullnessMeterBounds, fullnessMeterDraw, "liquidBar")
            .AddDynamicText(getContentsText(), CairoFont.WhiteDetailText(), barrelBoundsRight, "contentText")
            .EndChildElements()
            .Compose();
    }

    private string getContentsText()
    {
        string contents = Lang.Get("Contents:");
        if (Inventory[0].Empty && Inventory[1].Empty)
        {
            contents = contents + "\n" + Lang.Get("nobarrelcontents");
        }
        else
        {
            if (!Inventory[1].Empty)
            {
                ItemStack stack2 = Inventory[1].Itemstack;
                WaterTightContainableProps props2 = BlockLiquidContainerBase.GetContainableProps(stack2);
                if (props2 != null)
                {
                    string incontainername2 = Lang.Get(stack2.Collectible.Code.Domain + ":incontainer-" + stack2.Class.ToString().ToLowerInvariant() + "-" + stack2.Collectible.Code.Path);
                    contents = contents + "\n" + Lang.Get(props2.MaxStackSize > 0 ? "barrelcontents-items" : "barrelcontents-liquid", stack2.StackSize / props2.ItemsPerLitre, incontainername2);
                }
                else
                {
                    contents = contents + "\n" + Lang.Get("barrelcontents-items", stack2.StackSize, stack2.GetName());
                }
            }
            if (!Inventory[0].Empty)
            {
                ItemStack stack = Inventory[0].Itemstack;
                contents = contents + "\n" + Lang.Get("barrelcontents-items", stack.StackSize, stack.GetName());
            }
            BlockEntityWitchCauldron bebarrel = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as BlockEntityWitchCauldron;
            if (bebarrel.CurrentRecipe != null)
            {
                ItemStack outStack = bebarrel.CurrentRecipe.Output.ResolvedItemstack;
                WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(outStack);
                string timeText = bebarrel.CurrentRecipe.SealHours > 24.0 ? Lang.Get("{0} days", Math.Round(bebarrel.CurrentRecipe.SealHours / (double)capi.World.Calendar.HoursPerDay, 1)) : Lang.Get("{0} hours", bebarrel.CurrentRecipe.SealHours);
                if (props != null)
                {
                    string incontainername = Lang.Get(outStack.Collectible.Code.Domain + ":incontainer-" + outStack.Class.ToString().ToLowerInvariant() + "-" + outStack.Collectible.Code.Path);
                    float litres = bebarrel.CurrentOutSize / props.ItemsPerLitre;
                    contents = contents + "\n\n" + Lang.Get("Will turn into {0} litres of {1} after {2} of sealing.", litres, incontainername, timeText);
                }
                else
                {
                    contents = contents + "\n\n" + Lang.Get("Will turn into {0}x {1} after {2} of sealing.", bebarrel.CurrentOutSize, outStack.GetName(), timeText);
                }
            }
        }
        return contents;
    }

    public void UpdateContents()
    {
        SingleComposer.GetCustomDraw("liquidBar").Redraw();
        SingleComposer.GetDynamicText("contentText").SetNewText(getContentsText());
    }

    private void fullnessMeterDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
    {
        ItemSlot liquidSlot = Inventory[1];
        if (!liquidSlot.Empty)
        {
            BlockEntityWitchCauldron obj = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as BlockEntityWitchCauldron;
            float itemsPerLitre = 1f;
            int capacity = obj.CapacityLitres;
            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(liquidSlot.Itemstack);
            if (props != null)
            {
                itemsPerLitre = props.ItemsPerLitre;
                capacity = Math.Max(capacity, props.MaxStackSize);
            }
            float fullnessRelative = liquidSlot.StackSize / itemsPerLitre / capacity;
            double offY = (double)(1f - fullnessRelative) * currentBounds.InnerHeight;
            ctx.Rectangle(0.0, offY, currentBounds.InnerWidth, currentBounds.InnerHeight - offY);
            CompositeTexture tex = props?.Texture ?? liquidSlot.Itemstack.Collectible.Attributes?["inContainerTexture"].AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            if (tex != null)
            {
                ctx.Save();
                Matrix i = ctx.Matrix;
                i.Scale(GuiElement.scaled(3.0), GuiElement.scaled(3.0));
                ctx.Matrix = i;
                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");
                GuiElement.fillWithPattern(capi, ctx, loc, nearestScalingFiler: true, preserve: false, tex.Alpha);
                ctx.Restore();
            }
        }
    }

    private bool onSealClick()
    {
        if (!(capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) is BlockEntityWitchCauldron bebarrel) || bebarrel.Sealed)
        {
            return true;
        }
        if (!bebarrel.CanSeal)
        {
            return true;
        }
        bebarrel.SealBarrel();
        capi.Network.SendBlockEntityPacket(BlockEntityPosition, 1337);
        capi.World.PlaySoundAt(new AssetLocation("sounds/player/seal", "game"), BlockEntityPosition, 0.4);
        TryClose();
        return true;
    }

    private void SendInvPacket(object packet)
    {
        capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
    }

    private void OnTitleBarClose()
    {
        TryClose();
    }

    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        screenPos = GetFreePos("smallblockgui");
        OccupyPos("smallblockgui", screenPos);
        SetupDialog();
    }

    public override void OnGuiClosed()
    {
        SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
        base.OnGuiClosed();
        FreePos("smallblockgui", screenPos);
    }
}
