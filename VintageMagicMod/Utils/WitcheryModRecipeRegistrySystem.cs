using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VintageMagicMod.WitchCauldron.Recipes;
using VintageMagicMod.WitchOven.Recipes;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;


namespace VintageMagicMod.Utils
{
    public static partial class ApiAdditions
    {
        public static List<WitchOvenRecipe> GetWitchOvenRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<WitcheryModRecipeRegistrySystem>().WitchOvenRecipes;
        }

        /// <summary>
        /// Registers a knapping recipe. Only use it if you really want to avoid using json files for recipes. 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="r"></param>
        public static void RegisterWitchOvenRecipe(this ICoreServerAPI api, WitchOvenRecipe r)
        {
            api.ModLoader.GetModSystem<WitcheryModRecipeRegistrySystem>().RegisterWitchOvenRecipe(r);
        }


        public static List<WitchCauldronRecipe> GetWitchCauldronRecipes(this ICoreAPI api)
        {
            return api.ModLoader.GetModSystem<WitcheryModRecipeRegistrySystem>().WitchCauldronRecipes;
        }

        /// <summary>
        /// Registers a knapping recipe. Only use it if you really want to avoid using json files for recipes. 
        /// </summary>
        /// <param name="api"></param>
        /// <param name="r"></param>
        public static void RegisterWitchCauldronRecipe(this ICoreServerAPI api, WitchCauldronRecipe r)
        {
            api.ModLoader.GetModSystem<WitcheryModRecipeRegistrySystem>().RegisterWitchCauldronRecipe(r);
        }
    }

    public class DisableWitchOvenRecipeRegisteringSystem : ModSystem
    {
        public override double ExecuteOrder() => 99999;
        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;
        public override void AssetsFinalize(ICoreAPI api)
        {
            WitcheryModRecipeRegistrySystem.canRegister = false;
        }
    }

    public class WitcheryModRecipeRegistrySystem : ModSystem
    {
        public static bool canRegister = true;

        /// <summary>
        /// List of all loaded witch oven recipes
        /// </summary>
        public List<WitchOvenRecipe> WitchOvenRecipes = new List<WitchOvenRecipe>();

        /// <summary>
        /// List of all loaded witch cauldron recipes
        /// </summary>
        public List<WitchCauldronRecipe> WitchCauldronRecipes = new List<WitchCauldronRecipe>();

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

            WitchCauldronRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<WitchCauldronRecipe>>("WitchCauldronRecipes").Recipes;
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


        /// <summary>
        /// Registers a new witch cauldron recipe. These are sent to the client during connect, so only need to register them on the server side.
        /// </summary>
        /// <param name="recipe"></param>
        public void RegisterWitchCauldronRecipe(WitchCauldronRecipe recipe)
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

            WitchCauldronRecipes.Add(recipe);
        }
    }
}
