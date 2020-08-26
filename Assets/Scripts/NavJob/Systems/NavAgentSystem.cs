#region

using System.Collections.Concurrent;
using NavJob.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

#endregion

namespace NavJob.Systems
{

    ////[DisableAutoCreation]
    public class NavAgentSystem : JobComponentSystem
    {

        private EntityCommandBufferSystem EntityCommandBufferSystem;
        private NavMeshQuerySystem querySystem;


        private NativeQueue<AgentData> needsWaypoint;
        private ConcurrentDictionary<int, Vector3[]> waypoints = new ConcurrentDictionary<int, Vector3[]> ();
        private NativeHashMap<int, AgentData> pathFindingData;

        private EntityQuery queryUpdate;
       
        
        private struct AgentData
        {
            public int index;
            public Entity entity;
            public NavAgentComponent AgentComponent;
        }


      protected override void OnCreate ()
        {
            querySystem = EntityManager.World.GetOrCreateSystem<NavMeshQuerySystem>();
            querySystem.RegisterPathResolvedCallback (OnPathSuccess);
            querySystem.RegisterPathFailedCallback (OnPathError);
            needsWaypoint   = new NativeQueue<AgentData> (Allocator.Persistent);
            pathFindingData = new NativeHashMap<int, AgentData> (0, Allocator.Persistent);
            queryUpdate     = GetEntityQuery(typeof(NavAgentComponent));
        }

        protected override void OnDestroy()
        {
            needsWaypoint.Dispose ();
            pathFindingData.Dispose ();
        }
  

        protected override JobHandle OnUpdate (JobHandle inputDeps)
        {

            int navMeshQuerySystemVersion = querySystem.Version;
            var dt                        = Time.DeltaTime;
            
            JobHandle jobHandle01 = Entities
                .WithBurst()
                .WithAll<NavAgentComponent>()
                .ForEach((Entity entity, ref  NavAgentComponent agent) =>
                {
                    
                    if (agent.remainingDistance - agent.stoppingDistance > 0 || agent.status != AgentStatus.Moving)
                    {
                        return;
                    }
                    if (agent.nextWaypointIndex != agent.totalWaypoints)
                    {
                        agent.nextWayPoint = true;
                    }
                    else if (navMeshQuerySystemVersion != agent.queryVersion || agent.nextWaypointIndex == agent.totalWaypoints)
                    {
                        agent.totalWaypoints  = 0;
                        agent.currentWaypoint = 0;
                        agent.status          = AgentStatus.Idle;
                    }
                }).Schedule(inputDeps);



            
            
            JobHandle jobHandle02 = Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref  NavAgentComponent agent) =>
                {
                    if (agent.nextWayPoint)
                    {
                        NavAgentSystem navAgentSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<NavAgentSystem>();
                        if (navAgentSystem.waypoints.TryGetValue(entity.Index, out Vector3[] currentWaypoints))
                        {
                            agent.currentWaypoint   = currentWaypoints[agent.nextWaypointIndex];
                            agent.remainingDistance = Vector3.Distance(agent.position, agent.currentWaypoint);
                            agent.nextWaypointIndex++;
                            agent.nextWayPoint = false;
                        }
                    }
                }).Schedule(inputDeps);
                      
                      
                      
                      
            JobHandle jobHandle03 = Entities
                .WithBurst()
                .WithAll<NavAgentComponent>()
                .ForEach((Entity entity, ref  NavAgentComponent agent) =>
                {
                    if (agent.status != AgentStatus.Moving)
                    {
                        return;
                    }

                    if (agent.remainingDistance > 0)
                    {
                        agent.currentMoveSpeed = Mathf.Lerp (agent.currentMoveSpeed, agent.moveSpeed, dt * agent.acceleration);
                        // todo: deceleration
                        if (agent.nextPosition.x != Mathf.Infinity)
                        {
                            agent.position = agent.nextPosition;
                        }
                        var heading = (Vector3) (agent.currentWaypoint - agent.position);
                        agent.remainingDistance = heading.magnitude;
                        if (agent.remainingDistance > 0.001f)
                        {
                            var targetRotation                  = Quaternion.LookRotation (heading, Vector3.up).eulerAngles;
                            targetRotation.x = targetRotation.z = 0;
                            if (agent.remainingDistance < 1)
                            {
                                agent.rotation = Quaternion.Euler (targetRotation);
                            }
                            else
                            {
                                agent.rotation = Quaternion.Slerp (agent.rotation, Quaternion.Euler (targetRotation), dt * agent.rotationSpeed);
                            }
                        }
                        var forward = math.forward (agent.rotation) * agent.currentMoveSpeed * dt;
                        agent.nextPosition = agent.position + forward;
                    }
                    else if (agent.nextWaypointIndex == agent.totalWaypoints)
                    {
                        agent.nextPosition = new float3 { x = Mathf.Infinity, y = Mathf.Infinity, z = Mathf.Infinity };
                        agent.status       = AgentStatus.Idle;
                    
                    }
                    
                    
                }).Schedule(jobHandle02);
            
           
            return jobHandle03;
        }

        /// <summary>
        /// Used to set an agent destination and start the pathfinding process
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="agentComponent"></param>
        /// <param name="destination"></param>
        public void SetDestination (Entity entity, NavAgentComponent agentComponent, Vector3 destination, int areas = -1)
        {
            AgentData agentData = new AgentData {
                index = entity.Index, 
                entity = entity, 
                AgentComponent = agentComponent
            };
            if (pathFindingData.TryAdd (entity.Index, agentData))
            {
                agentComponent.status = AgentStatus.PathQueued;
                agentComponent.destination = destination;
                agentComponent.queryVersion = querySystem.Version;
                EntityManager.SetComponentData(entity, agentComponent);
                querySystem.RequestPath (entity.Index, agentComponent.position, agentComponent.destination, areas);
            }
        }

       

       
      

        private void SetWaypoint (Entity entity, NavAgentComponent agentComponent, Vector3[] newWaypoints)
        {
            waypoints[entity.Index] = newWaypoints;
            agentComponent.status = AgentStatus.Moving;
            agentComponent.nextWaypointIndex = 1;
            agentComponent.totalWaypoints = newWaypoints.Length;
            agentComponent.currentWaypoint = newWaypoints[0];
            agentComponent.remainingDistance = Vector3.Distance (agentComponent.position, agentComponent.currentWaypoint);
            EntityManager.SetComponentData(entity, agentComponent);
        }

        private void OnPathSuccess (int index, Vector3[] waypoints)
        {
            if (pathFindingData.TryGetValue (index, out AgentData entry))
            {
                SetWaypoint (entry.entity, entry.AgentComponent, waypoints);
                pathFindingData.Remove (index);
            }
        }

        private void OnPathError (int index, PathfindingFailedReason reason)
        {
            if (pathFindingData.TryGetValue (index, out AgentData entry))
            {
                entry.AgentComponent.status = AgentStatus.Idle;
                EntityManager.SetComponentData(entry.entity, entry.AgentComponent);
                pathFindingData.Remove (index);
            }
        }
    }
}