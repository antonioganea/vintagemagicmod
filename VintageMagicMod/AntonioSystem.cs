using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace VintageMagicMod
{
    internal class AntonioSystem : TemporarySystem
    {
        public void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockAntonioCauldron", typeof(BlockAntonioCauldron));
            api.RegisterBlockEntityClass("AntonioCauldron", typeof(BlockEntityAntonioCauldron));
        }
        public void StartServerSide(ICoreServerAPI api)
        {
            IChatCommand magicCommand = api.ChatCommands.Create("magic")
            .WithDescription("Magic system command")
            .RequiresPlayer()
            .RequiresPrivilege(Privilege.chat)
            .HandleWith((args) =>
            {
                return TextCommandResult.Success("I hope you are enjoying the magic mod.");
            });
        }
        public void StartClientSide(ICoreClientAPI api)
        {

        }

    }
}
