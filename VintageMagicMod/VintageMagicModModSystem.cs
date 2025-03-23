using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace VintageMagicMod
{

    public interface TemporarySystem
    {
        public void Start(ICoreAPI api);
        public void StartServerSide(ICoreServerAPI api);
        public void StartClientSide(ICoreClientAPI api);
    }

    public class VintageMagicModModSystem : ModSystem
    {
        public const string Domain = "vintagemagicmod";

        AntonioSystem antonio = new();
        DavidSystem david = new();
        NedasSystem nedas = new();
        NathanSystem nathan = new();

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from template mod: " + api.Side);

            antonio.Start(api);
            david.Start(api);
            nedas.Start(api);
            nathan.Start(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("vintagemagicmod:hello"));

            antonio.StartServerSide(api);
            david.StartServerSide(api);
            nedas.StartServerSide(api);
            nathan.StartServerSide(api);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("vintagemagicmod:hello"));

            antonio.StartClientSide(api);
            david.StartClientSide(api);
            nedas.StartClientSide(api);
            nathan.StartClientSide(api);
        }

    }
}
