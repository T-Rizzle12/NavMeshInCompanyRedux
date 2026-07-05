using BepInEx.Configuration;
using CSync.Extensions;
using CSync.Lib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NavMeshInCompanyRedux
{
    /// <summary>
    /// Config class, manage parameters editable by the player (irl)
    /// </summary>
    public class Config : SyncedConfig2<Config>
    {
        private const string ConfigSection = "NavMeshInCompany Redux";
        private const string ConfigDebug = "Debug";

        [SyncedEntryField] public SyncedEntry<bool> EnableDynamicRegen;
        [SyncedEntryField] public SyncedEntry<bool> DeferedDynamicRegen;
        public ConfigEntry<bool> EnableDebugLog;

        public Config(ConfigFile cfg) : base(MyPluginInfo.PLUGIN_GUID)
        {
            cfg.SaveOnConfigSet = false;

            EnableDynamicRegen = cfg.BindSyncedEntry(ConfigSection,
                                            "Enable Dynamic Nav Regen",
                                            defaultVal: true,
                                            "Should the NavMesh be dynamically regenerated upon landing? \n The regeneration will be done asynchronously! \n This allows mod added buildings to added to the NavMesh at runtime.");

            DeferedDynamicRegen = cfg.BindSyncedEntry(ConfigSection,
                                            "Defer Dynamic Regen",
                                            defaultVal: true,
                                            "Some mod added buildings are spawned after the scene is loaded. This may cause the NavMesh regeneration to be too early. \n This option will tell dynamic regeneration to wait a short delay before regenerating the NavMesh.");

            EnableDebugLog = cfg.Bind(ConfigDebug,
                                      "EnableDebugLog  (Client only)",
                                      defaultValue: false,
                                      "Enable the debug logs used for this mod.");

            ClearUnusedEntries(cfg);
            cfg.SaveOnConfigSet = true;
        }

        private void ClearUnusedEntries(ConfigFile cfg)
        {
            // Normally, old unused config entries don't get removed, so we do it with this piece of code. Credit to Kittenji.
            PropertyInfo orphanedEntriesProp = cfg.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg, null);
            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            cfg.Save(); // Save the config file to save these changes
        }
    }}
