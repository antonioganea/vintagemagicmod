using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;


namespace VintageMagicMod.WitchOven.Recipes
{
    public static partial class ApiAdditions
    {
        public static List<WitchOvenRecipe> GetWitchOvenRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<WitchOvenRecipeRegistrySystem>().WitchOvenRecipes;
        }

        /// <summary>
        /// Registers a knapping recipe. Only use it if you really want to avoid using json files for recipes. 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="r"></param>
        public static void RegisterWitchOvenRecipe(this ICoreServerAPI api, WitchOvenRecipe r)
        {
            api.ModLoader.GetModSystem<WitchOvenRecipeRegistrySystem>().RegisterWitchOvenRecipe(r);
        }
    }

    public class DisableWitchOvenRecipeRegisteringSystem : ModSystem
    {
        public override double ExecuteOrder() => 99999;
        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;
        public override void AssetsFinalize(ICoreAPI api)
        {
            WitchOvenRecipeRegistrySystem.canRegister = false;
        }
    }

    public class WitchOvenRecipeRegistrySystem : ModSystem
    {
        public static bool canRegister = true;

        /// <summary>
        /// List of all loaded witch oven recipes
        /// </summary>
        public List<WitchOvenRecipe> WitchOvenRecipes = new List<WitchOvenRecipe>();

        public override double ExecuteOrder()
        {
            return 0.6;
        }

        public override void StartPre(ICoreAPI api)
        {
            canRegister = true;
        }

        public override void Start(ICoreAPI api)
        {
            WitchOvenRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<WitchOvenRecipe>>("WitchOvenRecipes").Recipes;
        }

        /// <summary>
        /// Registers a new witch oven recipe. These are sent to the client during connect, so only need to register them on the server side.
        /// </summary>
        /// <param name="recipe"></param>
        public void RegisterWitchOvenRecipe(WitchOvenRecipe recipe)
        {
            if (!canRegister) throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
            if (recipe.Code == null)
            {
                throw new ArgumentException("Witch Oven recipes must have a non-null code! (choose freely)");
            }

            foreach (var ingred in recipe.Ingredients)
            {
                if (ingred.ConsumeQuantity != null && ingred.ConsumeQuantity > ingred.Quantity)
                {
                    throw new ArgumentException("Witch Oven recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
                }
            }

            WitchOvenRecipes.Add(recipe);
        }
    }
}
