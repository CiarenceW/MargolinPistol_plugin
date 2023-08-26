using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using Receiver2;
using System.Linq;
using UnityEngine;

namespace MargolinPistol_plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            StartCoroutine(WaitForObjectPoolIntialization());
        }

        private System.Collections.IEnumerator WaitForObjectPoolIntialization()
        {
            while (ObjectPool.pools == null)
            {
                yield return null;
            }
            AddMuzzleFlashToPool();
            yield break;
        }

        private void AddMuzzleFlashToPool()
        {
            var gun = ReceiverCoreScript.Instance().GetGunPrefab((GunModel)1041);
            var muzzleflash_pool = (from e in ObjectPool.pools where e.Key == "MuzzleFlashPool" select e.Value).First();
            gun.pooled_muzzle_flash.pool_prefab = muzzleflash_pool.gameObject;
            muzzleflash_pool.AddPrefab(gun.pooled_muzzle_flash.object_prefab);
            var pool_map = (Dictionary<string, int>)AccessTools.Field(typeof(ObjectPool), "pool_map").GetValue(muzzleflash_pool);
            var pool_map_index = pool_map.Values.Last() + 1;
            pool_map.Add(gun.pooled_muzzle_flash.object_prefab.name, pool_map_index);
            var muzzle_flash = GameObject.Instantiate(gun.pooled_muzzle_flash.object_prefab, muzzleflash_pool.transform);
            muzzle_flash.name = gun.pooled_muzzle_flash.object_prefab_name;
            muzzleflash_pool.pooled_prefab_parameters[pool_map_index].ClaimPool(muzzle_flash);
            Debug.LogFormat("Added {0} muzzleflash to pool", gun.InternalName);
        }
    }
}
