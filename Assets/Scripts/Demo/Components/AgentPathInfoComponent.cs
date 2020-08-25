using Unity.Entities;
using Unity.Mathematics;

namespace Demo.Components
{
    public struct AgentPathInfoComponent:IComponentData
    {
        public float3 startPos;
        public float3 endPos;
        public bool goes;
    }
}