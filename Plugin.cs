using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using LocalizationManager;
using PieceManager;
using ServerSync;

namespace KevinTree1
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class KevinTree1Plugin : BaseUnityPlugin
    {
        internal const string ModName = "KevinTree1";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Kevin1234";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource KevinTree1Logger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            // Uncomment the line below to use the LocalizationManager for localizing your mod.
            //Localizer.Load(); // Use this to initialize the LocalizationManager (for more information on LocalizationManager, see the LocalizationManager documentation https://github.com/blaxxun-boop/LocalizationManager#example-project).

            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            // Globally turn off configuration options for your pieces, omit if you don't want to do this.
            BuildPiece.ConfigurationEnabled = false;
            
            // Format: new("AssetBundleName", "PrefabName", "FolderName");
            BuildPiece tree1 = new("treetree123", "myfuntree1");
            tree1.Name.English("The_Tree007"); // Localize the name and description for the building piece for a language.
            tree1.Description.English("Ward For testing the Piece Manager");
            tree1.RequiredItems.Add("FineWood", 20, false); // Set the required items to build. Format: ("PrefabName", Amount, Recoverable)
            tree1.RequiredItems.Add("SurtlingCore", 20, false);
            tree1.Category.Add(PieceManager.BuildPieceCategory.Misc);
            tree1.Crafting.Set(PieceManager.CraftingTable.ArtisanTable); // Set a crafting station requirement for the piece.
            //tree1.Extension.Set(CraftingTable.Forge, 2); // Makes this piece a station extension, can change the max station distance by changing the second value. Use strings for custom tables.
            
            // Or you can do it for a custom table (### Default maxStationDistance is 5. I used 2 as an example here.)
            //tree1.Extension.Set("MYCUSTOMTABLE", 2); // Makes this piece a station extension, can change the max station distance by changing the second value. Use strings for custom tables.
            
            //tree1.Crafting.Set("CUSTOMTABLE"); // If you have a custom table you're adding to the game. Just set it like this.

            //tree1.SpecialProperties.NoConfig = true;  // Do not generate a config for this piece, omit this line of code if you want to generate a config.
            tree1.SpecialProperties = new SpecialProperties() { AdminOnly = true, NoConfig = true}; // You can declare multiple properties in one line           


            
            // If you want to add your item to the cultivator or another hammer with vanilla categories
            // Format: (AssetBundle, "PrefabName", addToCustom, "Item that has a piecetable")
                        
            // If you don't want to make an icon inside unity, but want the PieceManager to snag one for you, simply add .Snapshot() to your piece.
             // Optionally, you can use the lightIntensity parameter to set the light intensity of the snapshot. Default is 1.3 or the cameraRotation parameter to set the rotation of the camera. Default is null.

            // Need to add something to ZNetScene but not the hammer, cultivator or other? 
            // PiecePrefabManager.RegisterPrefab("bamboo", "Bamboo_Beam_Light");
            
            // Does your model need to swap materials with a vanilla material? Format: (GameObject, isJotunnMock)

            
            // Does your model use a shader from the game like Custom/Creature or Custom/Piece in unity? Need it to "just work"?
            //MaterialReplacer.RegisterGameObjectForShaderSwap(examplePiece3.Prefab, MaterialReplacer.ShaderType.UseUnityShader);
            
            // What if you want to use a custom shader from the game (like Custom/Piece that allows snow!!!) but your unity shader isn't set to Custom/Piece? Format: (GameObject, MaterialReplacer.ShaderType.)
            //MaterialReplacer.RegisterGameObjectForShaderSwap(examplePiece3.Prefab, MaterialReplacer.ShaderType.PieceShader);

            // Detailed instructions on how to use the MaterialReplacer can be found on the current PieceManager Wiki. https://github.com/AzumattDev/PieceManager/wiki

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                KevinTree1Logger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                KevinTree1Logger.LogError($"There was an issue loading your {ConfigFileName}");
                KevinTree1Logger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        #endregion
    }
}