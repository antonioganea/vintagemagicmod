using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageMagicMod
{
    public class VintageMagicModModSystem : ModSystem
    {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from template mod: " + api.Side);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("vintagemagicmod:hello"));

            IChatCommand magicCommand = api.ChatCommands.Create("magic")
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
