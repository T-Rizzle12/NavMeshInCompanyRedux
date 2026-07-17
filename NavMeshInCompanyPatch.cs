using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using SellCounterPatch = Kittenji.NavMeshInCompany.Plugin.DepositItemsDeskPatch;

namespace NavMeshInCompanyRedux
{
    [HarmonyPatch(typeof(SellCounterPatch))]
    public class NavMeshInCompanyPatch
    {
        [HarmonyPatch("StartPatch")]
        [HarmonyPrefix]
        public static bool StartPatch_Prefix()
        {
            //Plugin.LogInfo("Blocked NavMeshInCompany!");
            return false; // Block the original NavMeshInCompany from running!
        }
    }
}
