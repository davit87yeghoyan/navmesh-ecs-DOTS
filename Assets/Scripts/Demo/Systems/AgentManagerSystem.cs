using Demo.Components;
using NavJob.Components;
using NavJob.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Demo.Systems
{
    
    //[DisableAutoCreation]
    public class AgentManagerSystem:SystemBase
    {
        private NativeQueue<PipePassedEvent> eventQuery;
        private NavAgentSystem navAgentSystem;

        protected override void OnCreate()
        {
            eventQuery     = new NativeQueue<PipePassedEvent>(Allocator.TempJob);
            navAgentSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<NavAgentSystem>();
        }

        
       
        
        protected override void OnUpdate()
        {
            NativeQueue<PipePassedEvent>.ParallelWriter parallelWriter = eventQuery.AsParallelWriter();

            Entities.WithBurst().WithAll<NavAgentComponent>().ForEach((Entity e, ref AgentPathInfoComponent agentPathInfoComponentr, ref NavAgentComponent navAgent) =>
            {

                if (!agentPathInfoComponentr.goes && navAgent.status == AgentStatus.Idle){
                    navAgent.status              = AgentStatus.PathQueued;
                    agentPathInfoComponentr.goes = !agentPathInfoComponentr.goes;
                    Vector3 destination = agentPathInfoComponentr.goes ? agentPathInfoComponentr.endPos : agentPathInfoComponentr.startPos;

                    parallelWriter.Enqueue(new PipePassedEvent {
                        destination = destination,
                        Entity      = e,
                        navAgent    = navAgent,
                    });
                }
            }).Schedule();

            
            while (eventQuery.TryDequeue(out PipePassedEvent pipePassedEvent)){
                navAgentSystem.SetDestination(pipePassedEvent.Entity, pipePassedEvent.navAgent, pipePassedEvent.destination);
            }
            
        }
     
        
        public void CreateAgents(Entity entityPrefab, int countAgents)
        {
            NativeArray<Entity> entities = InstantiateEntityAgents(entityPrefab,countAgents);
            SetComponentInfo(entities,countAgents);
        }

        private void SetComponentInfo(NativeArray<Entity> entities, int countAgents)
        {
            for (int i = 0, index = 0; i < countAgents; i += Settings.LINE_WIDTH){
                for (int i1 = 0; i1 < Settings.LINE_WIDTH && index < countAgents; i1++, index++){
                    Entity entity   = entities[index];
                    float3 position = Settings.START_ADDING_POS + new float3(i1, 0, i/Settings.LINE_WIDTH);
                    SetPosition(entity,position);
                    InitComponentsAgent(entity,position);
                }
            }
        }
        
        
        private NativeArray<Entity> InstantiateEntityAgents(Entity entityPrefab, int countAgents)
        {
            NativeArray<Entity> entities = new NativeArray<Entity>(countAgents, Allocator.Temp);
            EntityManager.Instantiate(entityPrefab, entities);
            return entities;
        }

        private void SetPosition(Entity entity,float3 position)
        {
            EntityManager.SetComponentData(entity, new Translation {Value = position});
        }

        private void InitComponentsAgent(Entity entity, float3 position)
        {
            EntityManager.AddComponent(entity, typeof(SyncPositionFromNavAgentComponent));
            
            EntityManager.AddComponentData(entity, new AgentPathInfoComponent {
                goes = false,
                startPos = position,
                endPos = position + new float3(0,0,-Settings.LINE_HEIGTH),
            });

            
            var navAgentDefauult = EntityManager.GetComponentData<NavAgentComponent>(entity);
            var navAgent = new NavAgentComponent (
                position,
                Quaternion.identity,
                navAgentDefauult.stoppingDistance,
                navAgentDefauult.moveSpeed,
                navAgentDefauult.acceleration,
                navAgentDefauult.rotationSpeed,
                navAgentDefauult.areaMask
            );
            EntityManager.SetComponentData(entity, navAgent);
        }

      
    }

    internal struct PipePassedEvent
    {
        public Entity Entity;
        public NavAgentComponent navAgent;
        public float3 destination;
    }
}