using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using VintageMagicMod.WitchOven;
using VintageMagicMod.WitchCauldron;

namespace VintageMagicMod
{
    public class VintageMagicModModSystem : ModSystem
    {
        public const string Domain = "vintagemagicmod";

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from template mod: " + api.Side);

            api.RegisterItemClass(Domain + ".wooden_wand", typeof(ItemMagicWoodenWand));
            api.RegisterItemClass(Domain + ".bone_needle", typeof(ItemBoneNeedle));

            api.RegisterBlockClass("BlockAntonioCauldron", typeof(BlockAntonioCauldron));
            api.RegisterBlockEntityClass("AntonioCauldron", typeof(BlockEntityAntonioCauldron));

            api.RegisterBlockClass("BlockWitchOven", typeof(BlockWitchOven));
            api.RegisterBlockEntityClass("WitchOven", typeof(BlockEntityWitchOven));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("vintagemagicmod:hello"));

            IChatCommand magicCommand = api.ChatCommands.Create("magicCommand")
            .WithDescription("Magic system command")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith((args) =>
            {
                return TextCommandResult.Success("I hope you are enjoying the magic mod.");
            });
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("vintagemagicmod:hello"));
        }

    }
}
