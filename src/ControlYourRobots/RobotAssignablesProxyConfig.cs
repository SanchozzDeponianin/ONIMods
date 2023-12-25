using UnityEngine;

namespace ControlYourRobots
{
    public class RobotAssignablesProxyConfig : IEntityConfig
    {
        public static string ID = "RobotAssignablesProxy";

        public string[] GetDlcIds() => DlcManager.AVAILABLE_ALL_VERSIONS;

        public GameObject CreatePrefab()
        {
            var go = EntityTemplates.CreateEntity(ID, ID, true);
            go.AddOrGet<SaveLoadRoot>();
            go.AddOrGet<RobotIdentity>();
            go.AddOrGet<RobotAssignablesProxy>();
            return go;
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }
    }
}
