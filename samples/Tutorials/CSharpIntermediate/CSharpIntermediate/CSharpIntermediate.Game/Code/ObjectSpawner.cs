using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class ObjectSpawner : SyncScript
    {
        public Prefab ObjectToSpawn;

        private Entity prefabClone = null;

        public override void Start() { }

        public override void Update()
        {
            if (prefabClone == null)
            {
                DebugText.Print("Press S to spawn object", new Int2(300, 180));
            }

            if (Input.IsKeyPressed(Keys.S))
            {
                prefabClone = prefabClone ?? ObjectToSpawn.Instantiate().First();
                Entity.Transform.GetWorldTransformation(out Vector3 worldPos, out Quaternion rot, out Vector3 scale);
                prefabClone.Transform.Position = worldPos;
                prefabClone.Transform.UpdateWorldMatrix();

                prefabClone.Get<PhysicsComponent>().Enabled = true;
                Entity.Scene.Entities.Add(prefabClone);
            }
        }
    }
}
