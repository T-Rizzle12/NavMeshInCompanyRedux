using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace NavMeshInCompanyRedux
{
    [HarmonyPatch(typeof(RoundManager))]
    public class RoundManagerPatch
    {
        private const string COMPANY_BUILDING_MOON_SCENE_NAME = "71 Gordion.CompanyBuilding";

        [HarmonyPatch("FinishGeneratingNewLevelClientRpc")]
        [HarmonyPostfix]
        [HarmonyBefore(LethalBots.Plugin.ModGUID)]
        public static void FinishGeneratingNewLevelClientRpc_Postfix(RoundManager __instance)
        {
            // Only do this at the company building!
            SelectableLevel selectableLevel = StartOfRound.Instance.currentLevel;
            if ($"{selectableLevel.PlanetName}.{selectableLevel.sceneName}" != COMPANY_BUILDING_MOON_SCENE_NAME)
            {
                return;
            }

            try
            {
                Plugin.Logger.LogInfo("Instantiating Company Navigation!");
                Transform? tr = GameObject.Find("CompanyBuildingNavMesh")?.transform; // If we already have a navmesh, just use that one instead!
                if (tr == null)
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
                    tr = UnityEngine.Object.Instantiate(Plugin.CompanyBuildingNavMeshPrefab).transform;
                }

                // Make sure our prefab is lined up correctly!
                GameObject? navMeshColliders = GameObject.Find("NavMeshColliders");
                if (navMeshColliders != null)
                {
                    // Move to the NavMeshColliders object.
                    Plugin.Logger.LogInfo("Found NavMeshColliders!");
                    tr.SetParent(navMeshColliders.transform, worldPositionStays: true);
                    tr.position = navMeshColliders.transform.position;
                    tr.rotation = navMeshColliders.transform.rotation;

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
                    UnityEngine.Object.Destroy(tr.gameObject);
                    return;
                }

                // Update the AI nodes in the RoundManager to point to the new ones we just instantiated.
                Transform t = tr.Find("OutsideAINodes");
                __instance.outsideAINodes = new GameObject[t.childCount];
                for (int j = 0; j < t.childCount; j++)
                {
                    __instance.outsideAINodes[j] = t.GetChild(j).gameObject;
                }
                t = tr.Find("InsideAINodes");
                __instance.insideAINodes = new GameObject[t.childCount];
                for (int k = 0; k < t.childCount; k++)
                {
                    __instance.insideAINodes[k] = t.GetChild(k).gameObject;
                }

                // Update the NavMeshSurface component to ensure the navmesh is built and up-to-date.
                if (tr.gameObject.TryGetComponent(out NavMeshSurface navMeshSurface))
                {
                    // Can't rebuild at the minute, as it will break since some parts of the company building
                    // have read/write disabled on the mesh colliders, which will cause the navmesh to not be built correctly.
                    // TODO: Find a way to override the read/write disabled setting on the mesh colliders.
                    bool shouldRebuildNavMesh = false; // Not const to avoid compiler from removing the code below it.
                    if (shouldRebuildNavMesh)
                    {
                        return;
                    }

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
                }
                else
                {
                    Plugin.Logger.LogError("Failed to find NavMeshSurface component on the instantiated prefab.");
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.LogError($"Exception occurred: {exception}");
            }
        }
    }
}
