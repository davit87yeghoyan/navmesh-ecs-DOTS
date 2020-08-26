using NavJob.Components;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace NavJob.Systems
{
    [UpdateAfter(typeof(NavAgentSystem))]
    public class NavAgentSyncSystemFromNav : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Entities
                .WithBurst()
                .WithAll<SyncRotationFromNavAgentComponent>()
                .ForEach(( ref Rotation rotation,in NavAgentComponent agent) =>
                {
                    rotation.Value = agent.rotation;
                }).Schedule(inputDeps);

            Entities
                .WithBurst()
                .WithAll<SyncPositionFromNavAgentComponent>()
                .ForEach((ref Translation translation, in NavAgentComponent agent) =>
                {
                    translation.Value = agent.position;
                }).Schedule(inputDeps);
            return default;
        }
    }
    
    
}