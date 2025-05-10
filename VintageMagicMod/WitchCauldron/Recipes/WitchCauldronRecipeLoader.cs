using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VintageMagicMod.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace VintageMagicMod.WitchCauldron.Recipes
{
    public static partial class ApiAdditions
    {
        // TODO : Maybe we should take this function off of the ICoreAPI ? so we avoid cluttering..
        public static ItemStack TryBrewIngredients(this ICoreAPI api, ItemSlot[] inputSlots)
        {
            foreach (var recipe in api.GetWitchCauldronRecipes())
            {
                int outputStackSize;
                if (recipe.Matches(inputSlots, out outputStackSize))
                {
                    return recipe.Output.ResolvedItemstack;
                }
            }

            return null;
        }
    }

    public class WitchCauldronRecipeLoader : ModSystem
    {
        ICoreServerAPI api;

        public override double ExecuteOrder()
        {
            return 1;
        }

        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (!(api is ICoreServerAPI sapi)) return;
            this.api = sapi;

            LoadRecipes<WitchCauldronRecipe>("witchcauldron recipe", "recipes/witchcauldron", (r) => {
                sapi.RegisterWitchCauldronRecipe(r);
            });
        }

        public void LoadRecipes<T>(string name, string path, Action<T> RegisterMethod) where T : IRecipeBase<T>
        {
            Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, path, VintageMagicModModSystem.Domain);
            int recipeQuantity = 0;
            int quantityRegistered = 0;
            int quantityIgnored = 0;

            foreach (var val in files)
            {
                if (val.Value is JObject)
                {
                    LoadGenericRecipe(name, val.Key, val.Value.ToObject<T>(val.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
                    recipeQuantity++;
                }
                if (val.Value is JArray)
                {
                    foreach (var token in (val.Value as JArray))
                    {
                        LoadGenericRecipe(name, val.Key, token.ToObject<T>(val.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
                        recipeQuantity++;
                    }
                }
            }

            api.World.Logger.Event("{0} {1}s loaded{2}", quantityRegistered, name, quantityIgnored > 0 ? string.Format(" ({0} could not be resolved)", quantityIgnored) : "");
        }


        void LoadGenericRecipe<T>(string className, AssetLocation path, T recipe, Action<T> RegisterMethod, ref int quantityRegistered, ref int quantityIgnored) where T : IRecipeBase<T>
        {
            if (!recipe.Enabled) return;
            if (recipe.Name == null) recipe.Name = path;

            Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);

            if (nameToCodeMapping.Count > 0)
            {
                List<T> subRecipes = new List<T>();

                int qCombs = 0;
                bool first = true;
                foreach (var val2 in nameToCodeMapping)
                {
                    if (first) qCombs = val2.Value.Length;
                    else qCombs *= val2.Value.Length;
                    first = false;
                }

                first = true;
                foreach (var val2 in nameToCodeMapping)
                {
                    string variantCode = val2.Key;
                    string[] variants = val2.Value;

                    for (int i = 0; i < qCombs; i++)
                    {
                        T rec;

                        if (first) subRecipes.Add(rec = recipe.Clone());
                        else rec = subRecipes[i];

                        if (rec.Ingredients != null)
                        {
                            foreach (var ingred in rec.Ingredients)
                            {
                                if (ingred.Name == variantCode)
                                {
                                    ingred.Code = ingred.Code.CopyWithPath(ingred.Code.Path.Replace("*", variants[i % variants.Length]));
                                }
                            }
                        }

                        rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
                    }

                    first = false;
                }

                if (subRecipes.Count == 0)
                {
                    api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
                }

                foreach (T subRecipe in subRecipes)
                {
                    if (!subRecipe.Resolve(api.World, className + " " + path))
                    {
                        quantityIgnored++;
                        continue;
                    }
                    RegisterMethod(subRecipe);
                    quantityRegistered++;
                }

            }
            else
            {
                if (!recipe.Resolve(api.World, className + " " + path))
                {
                    quantityIgnored++;
                    return;
                }

                RegisterMethod(recipe);
                quantityRegistered++;
            }
        }
    }
}

