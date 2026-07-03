using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
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
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string COMPANY_BUILDING_MOON_SCENE_NAME = "71 Gordion.CompanyBuilding";

        public static AssetBundle ModAssets = null!;
        internal static string DirectoryName = null!;
        internal static GameObject CompanyBuildingNavMeshPrefab = null!;
        internal static new ManualLogSource Logger = null!;
        private readonly Harmony _harmony = new(MyPluginInfo.PLUGIN_GUID);

        private void Awake()
        {
            var bundleName = "companybuildingnavmesh";
            DirectoryName = Path.GetDirectoryName(Info.Location);

            Logger = base.Logger;

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
                Plugin.Logger.LogInfo("Instantiating Company Navigation!");
                Transform? companyTransform = GameObject.Find("CompanyBuildingNavMesh")?.transform; // If we already have a navmesh, just use that one instead!
                if (companyTransform == null)
                {
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
                    companyTransform = UnityEngine.Object.Instantiate(Plugin.CompanyBuildingNavMeshPrefab).transform;
                }

                // Make sure our prefab is lined up correctly!
                GameObject? navMeshColliders = GameObject.Find("NavMeshColliders");
                if (navMeshColliders != null)
                {
                    // Move to the NavMeshColliders object.
                    Plugin.Logger.LogInfo("Found NavMeshColliders!");
                    companyTransform.SetParent(navMeshColliders.transform, worldPositionStays: true);
                    companyTransform.position = navMeshColliders.transform.position;
                    companyTransform.rotation = navMeshColliders.transform.rotation;

                    Transform oldShipNav = navMeshColliders.transform.Find("PlayerShip");
                    if (oldShipNav != null)
                    {
                        Plugin.Logger.LogInfo("Found old ShipNavmesh, destroying it!");
                        UnityEngine.Object.DestroyImmediate(oldShipNav.gameObject);
                    }
                }
                else
                {
                    // As where it is located in the Unity Editor.
                    Plugin.Logger.LogWarning("Failed to find NavMeshColliders!");
                    UnityEngine.Object.Destroy(companyTransform.gameObject);
                    return;
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
                // Can't rebuild at the minute, as it will break since some parts of the company building
                // have read/write disabled on the mesh colliders, which will cause the navmesh to not be built correctly.
                // TODO: Find a way to override the read/write disabled setting on the mesh colliders.
                bool shouldRebuildNavMesh = false; // Not const to avoid compiler from removing the code below it.
                if (shouldRebuildNavMesh)
                {
                    NavMeshSurface[] navMeshSurfaces = companyTransform.gameObject.GetComponentsInChildren<NavMeshSurface>(includeInactive: true);
                    if (navMeshSurfaces == null || navMeshSurfaces.Length == 0)
                    {
                        Plugin.Logger.LogError("Failed to find any NavMeshSurface components on the instantiated prefab.");
                        return;
                    }

                    foreach (NavMeshSurface navMeshSurface in navMeshSurfaces)
                    {
                        // Make this is enabled!
                        bool wasEnabled = navMeshSurface.enabled;
                        navMeshSurface.enabled = true;

                        // If the navmesh data already exists, we can use async update to avoid blocking the main thread.
                        // If it doesn't exist, we need to build it synchronously.
                        // TODO: Look into how BuildNavMesh creates the NavMeshData and see if we could build our own
                        // to avoid the synchronous call.
                        NavMeshData? navMeshData = navMeshSurface.navMeshData;
                        if (navMeshData != null)
                        {
                            navMeshSurface.UpdateNavMesh(navMeshData);
                        }
                        else
                        {
                            navMeshSurface.BuildNavMesh();
                        }

                        // Set enabled status back to how it was before
                        navMeshSurface.enabled = wasEnabled;
                    }
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
    }
}
