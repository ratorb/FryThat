global using static JustWingIt.Main;
global using static KitchenLib.Utils.GDOUtils;
global using static KitchenLib.Utils.LocalisationUtils;
using JustWingIt.Wings.Intermediate;
using JustWingIt.Wings.Intermediate.Nuggets;
using Kitchen;
using KitchenData;
using KitchenLib;
using KitchenLib.Customs;
using KitchenLib.Event;
using KitchenLib.Utils;
using KitchenMods;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static KitchenLib.Utils.MaterialUtils;

namespace JustWingIt
{
    public class Main : BaseMod
    {
        public const string GUID = "nova.justwingit";
        public const string VERSION = "1.1.3";

        public Main() : base(GUID, "Fry That!", "Zoey Davis", VERSION, ">=1.0.0", Assembly.GetExecutingAssembly()) { }

        private static AssetBundle Bundle;
        
        private void PreBuild()
        {

        }

        private void PostBuild()
        {

        }

        #region Multi-GDO Prefabs
        internal static void SetupDeepFryPot(GameObject prefab)
        {
            var view = prefab.TryAddComponent<ItemGroupView>();
            if (!view.ComponentGroups.IsNullOrEmpty())
                return;

            GetChickenMaterial("Fried", 0xCA8243);

            prefab.ApplyMaterialToChild("Pot/Base", "Metal");
            prefab.ApplyMaterialToChild("Pot/Handle", "Metal Dark");

            prefab.ApplyMaterialToChild("Oil", "Frying Oil");

            SetupType(prefab.GetChild("Chicken"), false);

            SetupType(prefab.GetChild("Nuggets"), true);

            view.ComponentGroups = new()
            {
                new()
                {
                    Item = GetCastedGDO<Item, FriedWing>(),
                    Objects = new()
                    {
                        prefab.GetChild("Chicken/Fried/Chicken 3"),
                        prefab.GetChild("Chicken/Fried/Chicken 2"),
                        prefab.GetChild("Chicken/Fried/Chicken 1")
                    }
                },
                new()
                {
                    Item = GetCastedGDO<Item, FlouredWing>(),
                    Objects = new()
                    {
                        prefab.GetChild("Chicken/Coated/Chicken 1"),
                        prefab.GetChild("Chicken/Coated/Chicken 2"),
                        prefab.GetChild("Chicken/Coated/Chicken 3")
                    }
                },
                new()
                {
                    Item = GetCastedGDO<Item, BurnedWing>(),
                    Objects = new()
                    {
                        prefab.GetChild("Chicken/Burned/Chicken 1"),
                        prefab.GetChild("Chicken/Burned/Chicken 2"),
                        prefab.GetChild("Chicken/Burned/Chicken 3")
                    }
                },
                new()
                {
                    Item = GetCastedGDO<Item, FriedNugget>(),
                    Objects = new()
                    {
                        prefab.GetChild("Nuggets/Fried/Chicken 3"),
                        prefab.GetChild("Nuggets/Fried/Chicken 2"),
                        prefab.GetChild("Nuggets/Fried/Chicken 1")
                    }
                },
                new()
                {
                    Item = GetCastedGDO<Item, FlouredNugget>(),
                    Objects = new()
                    {
                        prefab.GetChild("Nuggets/Coated/Chicken 1"),
                        prefab.GetChild("Nuggets/Coated/Chicken 2"),
                        prefab.GetChild("Nuggets/Coated/Chicken 3")
                    }
                },
                new()
                {
                    Item = GetCastedGDO<Item, BurnedNugget>(),
                    Objects = new()
                    {
                        prefab.GetChild("Nuggets/Burned/Chicken 1"),
                        prefab.GetChild("Nuggets/Burned/Chicken 2"),
                        prefab.GetChild("Nuggets/Burned/Chicken 3")
                    }
                },
            };
        }
        private static void SetupType(GameObject parent, bool nugget)
        {
            parent.GetChild("Fried").ApplyMaterialToChildren("", "Wing - Fried");
            parent.GetChild("Burned").ApplyMaterialToChildren("", "Burned");

            var coated = parent.GetChild("Coated");
            for (int i = 0; i < 3; i++)
            {
                if (!nugget)
                    coated.GetChild(i).ApplyMaterialToChildren("", "Crab - Raw Meat");
                else
                    coated.GetChild(i).ApplyMaterial("Crab - Raw Meat");
            }
        }
        #endregion

        #region Registry
        protected override void OnPostActivate(Mod mod)
        {
            Bundle = mod.GetPacks<AssetBundleModPack>().SelectMany(e => e.AssetBundles).ToList()[0];

            PreBuild();

            AddGameData();

            Events.BuildGameDataEvent += (s, args) =>
            {
                PostBuild();

                args.gamedata.ProcessesView.Initialise(args.gamedata);
            };

        }

        internal void AddGameData()
        {
            MethodInfo AddGDOMethod = typeof(BaseMod).GetMethod(nameof(BaseMod.AddGameDataObject));
            int counter = 0;
            LogInfo("Registering GameDataObjects.");
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsAbstract || typeof(IWontRegister).IsAssignableFrom(type))
                    continue;

                if (!typeof(CustomGameDataObject).IsAssignableFrom(type))
                    continue;

                MethodInfo generic = AddGDOMethod.MakeGenericMethod(type);
                generic.Invoke(this, null);
                counter++;
            }
            LogInfo($"Registered {counter} GameDataObjects.");
        }

        public interface IWontRegister { }
        #endregion

        #region Logging
        internal static void LogInfo(string msg) { Debug.Log($"[{GUID}] " + msg); }
        internal static void LogWarning(string msg) { Debug.LogWarning($"[{GUID}] " + msg); }
        internal static void LogError(string msg) { Debug.LogError($"[{GUID}] " + msg); }
        internal static void LogInfo(object msg) { LogInfo(msg.ToString()); }
        internal static void LogWarning(object msg) { LogWarning(msg.ToString()); }
        internal static void LogError(object msg) { LogError(msg.ToString()); }
        #endregion

        #region Utility
        public static T GetGDO<T>(int id) where T : GameDataObject => GetExistingGDO(id) as T;

        public static GameObject GetPrefab(string name) => Bundle.LoadAsset<GameObject>(name);
        public static T GetAsset<T>(string name) where T : Object => Bundle.LoadAsset<T>(name);

        public static Material GetChickenMaterial(string name, int color)
        {
            if (TryGetMaterial("Wing - " + name, out var x))
                return x;

            Color col = ColorFromHex(color);
            Material mat = new Material(GetExistingMaterial("Well-done  Burger"))
            {
                name = "Wing - " + name
            };
            mat.SetColor("_Color0", col);
            CustomMaterials.AddMaterial("Wing - " + name, mat);
            return mat;
        }

        public static void TryAddMaterial(string name, int color, float shininess = 0, float overlayScale = 10)
        {
            if (CustomMaterials.CustomMaterialsIndex.ContainsKey(name))
                return;

            CustomMaterials.AddMaterial(name, CreateFlat(name, color, shininess, overlayScale));
        }

        private static bool TryGetMaterial(string name, out Material material)
        {
            if (CustomMaterials.CustomMaterialsIndex.ContainsKey(name))
            {
                material = CustomMaterials.CustomMaterialsIndex[name];
                return true;
            }
            material = null;
            return false;
        }
        #endregion

        #region References
        public const int OilPot = 1719428613;
        public const int ChoppedLemon = 741084967;

        internal static readonly RestaurantStatus NUGGET_STATUS = (RestaurantStatus)VariousUtils.GetID("JWI:Nuggets");
        #endregion
    }
}
