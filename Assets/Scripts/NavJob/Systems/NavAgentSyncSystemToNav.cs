using NavJob.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace NavJob.Systems
{
    /// <summary>
    /// Sets the Rotation component to the NavAgent rotation
    /// </summary>
    [UpdateBefore(typeof(NavAgentSystem))]
    //[DisableAutoCreation]
    public class NavAgentSyncSystemToNav : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
          
            
            Entities
                .WithBurst()
                .WithAll<SyncRotationToNavAgentComponent>()
                .ForEach((ref NavAgentComponent agent, in Rotation rotation) =>
                {
                    agent.rotation = rotation.Value;
                }).Schedule(inputDeps);


            Entities
                .WithBurst()
                .WithAll<SyncPositionToNavAgentComponent>()
                .ForEach((ref NavAgentComponent agent, in Translation translation) =>
                {
                    agent.position = translation.Value;
                }).Schedule(inputDeps);
            return default;
        }
    }
}