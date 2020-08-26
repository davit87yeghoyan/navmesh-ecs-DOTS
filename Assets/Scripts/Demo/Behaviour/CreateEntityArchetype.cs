using Demo.Systems;
using NavJob.Components;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Demo.Behaviour
{
    public class CreateEntityArchetype: MonoBehaviour
    {
        private void Awake()
        {
            EntityManager EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityArchetype entityArchetype = EntityManager.CreateArchetype(
                typeof(SyncPositionFromNavAgentComponent),
                typeof(SyncPositionToNavAgentComponent),
                typeof(SyncRotationFromNavAgentComponent),
                typeof(SyncRotationToNavAgentComponent),
                typeof(Rotation),
                typeof(Transform),
                typeof(NavAgentComponent),
                typeof(PipePassedEvent)
            );
            EntityManager.CreateEntity(entityArchetype);
        }
    }
}