using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class ItemVoodooPoppet : Item
{
    public Entity storedEntity;


    // You can add more functionalities to the VoodooPoppet based on the stored entity here
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

        IWorldAccessor world = byEntity.World;

        // Example: Show debug info about captured entity
        if (storedEntity != null)
        {
            world.Logger.Debug($"VoodooPoppet contains essence of: {storedEntity.Code}");

            if (byEntity is IPlayer player)
            {
                world.Logger.Debug($"Poppet contains essence of: {storedEntity.Code}");

            }

            handling = EnumHandHandling.PreventDefault;
        }
        else
        {
            if (byEntity is IPlayer player)
            {
                world.Logger.Debug("This poppet has not captured any entity yet.");
            }
        }
    }
}
