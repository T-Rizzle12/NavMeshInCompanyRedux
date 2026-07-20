using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace NavMeshInCompanyRedux
{
    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID = "T-Rizzle.NavMeshInCompanyRedux";
        public const string PLUGIN_NAME = "NavMeshInCompanyRedux";
        public const string PLUGIN_VERSION = "1.2.0";
    }

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.sigurd.csync", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(Kittenji.NavMeshInCompany.Plugin.pluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string COMPANY_BUILDING_MOON_SCENE_NAME = "71 Gordion.CompanyBuilding";

        public static AssetBundle ModAssets = null!;
        internal static string DirectoryName = null!;
        internal static GameObject CompanyBuildingNavMeshPrefab = null!;

        internal static new ManualLogSource Logger = null!;
        public static new Config Config = null!;
        
        private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            var bundleName = "companybuildingnavmesh";
            DirectoryName = Path.GetDirectoryName(Info.Location);

            Logger = base.Logger;
            Config = new Config(base.Config);

            // Load mod assets from Unity
            ModAssets = AssetBundle.LoadFromFile(Path.Combine(DirectoryName, bundleName));
            if (ModAssets == null)
            {
                Logger.LogFatal($"Unknown to load custom assets.");
                return;
            }

            // Load the nav mesh prefab from the asset bundle
            CompanyBuildingNavMeshPrefab = ModAssets.LoadAsset<GameObject>("CompanyBuildingNavMesh");
            if (CompanyBuildingNavMeshPrefab == null)
            {
                Logger.LogFatal($"Failed to load the nav mesh prefab from the asset bundle.");
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Override the default NavMeshInCompany
            try
            {
                if (Chainloader.PluginInfos.ContainsKey(Kittenji.NavMeshInCompany.Plugin.pluginGUID))
                {
                    _harmony.PatchAll(typeof(NavMeshInCompanyPatch));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to patch with error {ex}");
            }

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Only do this for the company building.
            StartOfRound instanceSOR = StartOfRound.Instance;
            if (instanceSOR == null
                || instanceSOR.currentLevel == null)
            {
                return;
            }

            string levelName = $"{instanceSOR.currentLevel.PlanetName}.{instanceSOR.currentLevel.sceneName}";
            if (levelName != COMPANY_BUILDING_MOON_SCENE_NAME)
            {
                return;
            }

            try
            {
                Plugin.LogInfo("Instantiating Company Navigation!");

                // We can't rebuild with some of the default Meshes, as it will break dynamic regeneration since some parts of the company building
                // have read/write disabled on the mesh colliders, which will cause the navmesh to not be built correctly.
                GameObject[] sceneObjects = scene.GetRootGameObjects();
                if (sceneObjects != null && sceneObjects.Length > 0) 
                {
                    // Check all the root objects in the scene for any MeshFilters that need to be replaced with our own meshes.
                    for (int i = 0; i < sceneObjects.Length; i++)
                    {
                        // Sanity check for null, just in case.
                        GameObject gameObject = sceneObjects[i];
                        if (gameObject != null)
                        {
                            // Get all MeshFilters in the root object and its children, including inactive ones.
                            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(includeInactive: true);
                            for (int j = 0; j < meshFilters.Length; j++)
                            {
                                // Sanity check for null, just in case.
                                MeshFilter meshFilter = meshFilters[j];
                                if (meshFilter != null)
                                {
                                    int instanceID = meshFilter.gameObject.GetInstanceID();
                                    string objectName = meshFilter.gameObject.name;
                                    //Plugin.LogDebug($"Found GameObject: {objectName} with InstanceID: {instanceID}");
                                    if (objectName.Equals("Cube.005", StringComparison.InvariantCultureIgnoreCase)) // Cube.005 instanceID == 159178
                                    {
                                        //Mesh newMesh = ModAssets.LoadAsset<Mesh>("Cube.005");
                                        //Material[] oldMaterials = meshRenderer.materials;
                                        //GameObject replacementObject = new GameObject("Cube.005");
                                        //replacementObject.AddComponent<MeshRenderer>().materials = oldMaterials;
                                        //replacementObject.AddComponent<MeshFilter>().sharedMesh = newMesh;
                                        //replacementObject.AddComponent<MeshCollider>().sharedMesh = newMesh;
                                        Mesh newMesh = ModAssets.LoadAsset<Mesh>("Cube.005");
                                        meshFilter.sharedMesh = newMesh;
                                        meshFilter.gameObject.GetComponent<MeshCollider>()?.sharedMesh = newMesh;
                                        Plugin.LogDebug($"Replaced mesh for {objectName} with new mesh: {newMesh.name}. Read/Write {newMesh.isReadable}");
                                    }
                                    //else if (instanceID == 157752) // Cube 157752, old 158678
                                    //{
                                    //    Mesh newMesh = ModAssets.LoadAsset<Mesh>("Cube");
                                    //    meshFilter.sharedMesh = newMesh;
                                    //    meshFilter.gameObject.GetComponent<MeshCollider>()?.sharedMesh = newMesh;
                                    //    Plugin.LogInfo($"Replaced mesh for {meshFilter.gameObject.name} with new mesh: {newMesh.name}. Read/Write {newMesh.isReadable}");
                                    //}
                                    else if (objectName.Equals("Cylinder", StringComparison.InvariantCultureIgnoreCase)) // Cylinder instanceID == 158970
                                    {
                                        Mesh newMesh = ModAssets.LoadAsset<Mesh>("Cylinder");
                                        meshFilter.sharedMesh = newMesh;
                                        meshFilter.gameObject.GetComponent<MeshCollider>()?.sharedMesh = newMesh;
                                        Plugin.LogDebug($"Replaced mesh for {objectName} with new mesh: {newMesh.name}. Read/Write {newMesh.isReadable}");
                                    }
                                    else if (objectName.Equals("DrillPlatform", StringComparison.InvariantCultureIgnoreCase)) // DrillPlatform instanceID == 158940
                                    {
                                        Mesh newMesh = ModAssets.LoadAsset<Mesh>("DrillPlatform");
                                        meshFilter.sharedMesh = newMesh;
                                        meshFilter.gameObject.GetComponent<MeshCollider>()?.sharedMesh = newMesh;
                                        Plugin.LogDebug($"Replaced mesh for {objectName} with new mesh: {newMesh.name}. Read/Write {newMesh.isReadable}");
                                    }
                                    else if (objectName.Equals("Elbow Joint.001", StringComparison.InvariantCultureIgnoreCase)) // Elbow Joint.001 instanceID == 158958
                                    {
                                        Mesh newMesh = ModAssets.LoadAsset<Mesh>("Elbow Joint.001");
                                        meshFilter.sharedMesh = newMesh;
                                        meshFilter.gameObject.GetComponent<MeshCollider>()?.sharedMesh = newMesh;
                                        Plugin.LogDebug($"Replaced mesh for {objectName} with new mesh: {newMesh.name}. Read/Write {newMesh.isReadable}");
                                    }
                                }
                            }
                        }
                    }
                }

                // Check if we already have a navmesh in the scene, if so, just use that one instead of instantiating a new one.
                Transform? companyTransform = GameObject.Find("CompanyBuildingNavMesh")?.transform; // If we already have a navmesh, just use that one instead!
                if (companyTransform == null)
                {
                    // Destroy any existing AI nodes in the scene, as they will be replaced by the new ones in our prefab.
                    GameObject[] array = GameObject.FindGameObjectsWithTag("OutsideAINode");
                    for (int i = 0; i < array.Length; i++)
                    {
                        UnityEngine.Object.Destroy(array[i]);
                    }
                    array = GameObject.FindGameObjectsWithTag("AINode");
                    for (int i = 0; i < array.Length; i++)
                    {
                        UnityEngine.Object.Destroy(array[i]);
                    }
                    companyTransform = ((GameObject)UnityEngine.Object.Instantiate(Plugin.CompanyBuildingNavMeshPrefab, scene)).transform;
                }

                // Make sure our prefab is lined up correctly!
                GameObject? environmentObject = sceneObjects.FirstOrDefault(x => x.name.Equals("Environment", StringComparison.InvariantCultureIgnoreCase));
                Transform? navMeshColliders = environmentObject != null ? environmentObject.transform.Find("NavMeshColliders") : null;
                if (navMeshColliders != null)
                {
                    // Move to the NavMeshColliders object.
                    Plugin.LogInfo("Found NavMeshColliders!");
                    Transform oldShipNav = navMeshColliders.Find("PlayerShip");
                    companyTransform.position = navMeshColliders.transform.position;
                    companyTransform.rotation = navMeshColliders.transform.rotation;
                    if (oldShipNav != null)
                    {
                        Plugin.LogInfo("Found old ShipNavmesh, destroying it!");
                        UnityEngine.Object.DestroyImmediate(oldShipNav.gameObject);
                    }
                }
                else
                {
                    // As where it is located in the Unity Editor.
                    // HACKHACK: For some reason, the position offsets I put in the editor don't want to be loaded here.
                    // So I force them to work
                    companyTransform.position = new Vector3(-17.4388561f, 7.60538054f, -16.4713039f);
                    companyTransform.rotation = Quaternion.identity;
                    Plugin.LogWarning("Failed to find NavMeshColliders!");
                }

                // Update the AI nodes in the RoundManager to point to the new ones we just instantiated.
                RoundManager instanceRM = RoundManager.Instance;
                Transform t = companyTransform.Find("OutsideAINodes");
                instanceRM.outsideAINodes = new GameObject[t.childCount];
                for (int j = 0; j < t.childCount; j++)
                {
                    instanceRM.outsideAINodes[j] = t.GetChild(j).gameObject;
                }
                t = companyTransform.Find("InsideAINodes");
                instanceRM.insideAINodes = new GameObject[t.childCount];
                for (int k = 0; k < t.childCount; k++)
                {
                    instanceRM.insideAINodes[k] = t.GetChild(k).gameObject;
                }

                // Update the NavMeshSurface component to ensure the navmesh is built and up-to-date.
                if (Plugin.Config.EnableDynamicRegen.Value)
                {
                    NavMeshSurface[] navMeshSurfaces = companyTransform.gameObject.GetComponentsInChildren<NavMeshSurface>(includeInactive: true);
                    if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
                    {
                        Plugin.LogError("Failed to find any NavMeshSurface components on the instantiated prefab.");
                        return;
                    }

                    instanceRM.StartCoroutine(UpdateNavmeshDelayed(companyTransform, navMeshSurfaces));
                    return;
                }

                // Now, we need to update start and endpoints of the NavMeshLink and OffMeshLink components.
                NavMeshLink[] navMeshLinks = companyTransform.gameObject.GetComponentsInChildren<NavMeshLink>(includeInactive: true);
                foreach (NavMeshLink navMeshLink in navMeshLinks)
                {
                    navMeshLink.UpdateLink();
                }

                // Until Zeekerss stops using the obsolete OffMeshLink component, we need to disable the warning for it.
                #pragma warning disable CS0618 // Type or member is obsolete
                OffMeshLink[] offMeshLinks = companyTransform.gameObject.GetComponentsInChildren<OffMeshLink>(includeInactive: true);
                foreach (OffMeshLink offMeshLink in offMeshLinks)
                {
                    offMeshLink.UpdatePositions();
                }
                #pragma warning restore CS0618 // Type or member is obsolete
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError($"Exception occurred: {exception}");
            }
        }

        private static IEnumerator UpdateNavmeshDelayed(Transform? companyTransform, NavMeshSurface[] surfacesToRebake)
        {
            // Wait for a short delay to allow any mod added buildings to be spawned before we rebuild the navmesh.
            if (Plugin.Config.DeferedDynamicRegen.Value)
            {
                // Most mod-added buildings are spawned after the "level" generation is complete.
                // The base game uses a 0.3 second delay before marking generation as complete.
                // So this delay should be long enough to allow any mod added buildings to be spawned before we rebuild the navmesh.
                Plugin.LogDebug("Waiting 1 second before rebaking the navmesh to allow any mod added buildings to be spawned.");
                yield return new WaitForSeconds(1.0f);
            }

            // Lets go and rebake the navmesh for all the surfaces we found!
            foreach (var navMeshSurface in surfacesToRebake)
            {
                if (navMeshSurface != null)
                {
                    // Log about what we are updating!
                    Plugin.LogDebug($"Updating NavMesh for surface {navMeshSurface.gameObject.name} with {navMeshSurface.GetComponentsInChildren<NavMeshModifierVolume>().Length} modifiers.");

                    // Make this is enabled!
                    bool wasEnabled = navMeshSurface.enabled;
                    navMeshSurface.enabled = true;

                    // Build our new mesh!
                    // If the navmesh data already exists, we can use async update to avoid blocking the main thread.
                    NavMeshData? navMeshData = navMeshSurface.navMeshData;
                    if (navMeshData != null)
                    {
                        AsyncOperation asyncOperation = navMeshSurface.UpdateNavMesh(navMeshData);
                        while (asyncOperation != null && !asyncOperation.isDone)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        navMeshSurface.BuildNavMesh();
                    }

                    // Update the NavMeshData!
                    Plugin.LogDebug($"UpdateNavMesh finished, refreshing surface data.");
                    navMeshSurface.RemoveData();
                    Plugin.LogDebug("Removed existing data.");

                    // Only add the data back if it was enabled before,
                    // otherwise we will leave it disabled.
                    if (wasEnabled)
                    {
                        navMeshSurface.AddData();
                        Plugin.LogDebug("Added updated data.");
                    }

                    // Set enabled status back to how it was before
                    navMeshSurface.enabled = wasEnabled;
                }
                else
                {
                    Plugin.LogWarning("Found a null NavMeshSurface in the list to rebake, skipping it.");
                }
            }

            // Now, we need to update start and endpoints of the NavMeshLink and OffMeshLink components.
            if (companyTransform)
            {
                NavMeshLink[] navMeshLinks = companyTransform.gameObject.GetComponentsInChildren<NavMeshLink>(includeInactive: true);
                foreach (NavMeshLink navMeshLink in navMeshLinks)
                {
                    navMeshLink.UpdateLink();
                }

                // Until Zeekerss stops using the obsolete OffMeshLink component, we need to disable the warning for it.
                #pragma warning disable CS0618 // Type or member is obsolete
                OffMeshLink[] offMeshLinks = companyTransform.gameObject.GetComponentsInChildren<OffMeshLink>(includeInactive: true);
                foreach (OffMeshLink offMeshLink in offMeshLinks)
                {
                    offMeshLink.UpdatePositions();
                }
                #pragma warning restore CS0618 // Type or member is obsolete
            }

            Plugin.LogDebug("Updated all given NavMeshes.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogDebug(string debugLog)
        {
            if (Config.EnableDebugLog.Value)
            {
                Logger.LogDebug(debugLog);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogInfo(string infoLog)
        {
            Logger.LogInfo(infoLog);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogWarning(string warningLog)
        {
            Logger.LogWarning(warningLog);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogError(string errorLog)
        {
            Logger.LogError(errorLog);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void LogFatal(string errorLog)
        {
            Logger.LogFatal(errorLog);
        }
    }
}