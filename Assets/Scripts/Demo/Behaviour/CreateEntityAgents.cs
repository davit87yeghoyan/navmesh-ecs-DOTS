using Demo.Systems;
using Unity.Entities;
using UnityEngine;

namespace Demo.Behaviour
{
    public class CreateEntityAgents : MonoBehaviour
    {
        public GameObject AgentPrefab;
        public int CountAgents = 1000;

        

        void Awake()
        {
            
            using (BlobAssetStore blobAssetStore = new BlobAssetStore()){
                var    setting            = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
                Entity entityPrefab       = GameObjectConversionUtility.ConvertGameObjectHierarchy(AgentPrefab, setting);
                AgentManagerSystem    agentManagerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<AgentManagerSystem>();
                agentManagerSystem.CreateAgents(entityPrefab, CountAgents);
                //agentManagerSystem.Materials = Materials;
            }
        }

  

   



   
    }
}