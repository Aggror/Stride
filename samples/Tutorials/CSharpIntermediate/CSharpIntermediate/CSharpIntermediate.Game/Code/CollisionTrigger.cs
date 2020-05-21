using System.Collections.Specialized;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class CollisionTrigger : SyncScript
    {
        PhysicsComponent collider;
        string enterStatus = "";
        string exitStatus = "";

        public override void Start()
        {
            collider = Entity.Get<PhysicsComponent>();
            collider.Collisions.CollectionChanged += collisionsChanged;
        }

        private void collisionsChanged(object sender, TrackingCollectionChangedEventArgs args)
        {
            var collision = (Collision)args.Item;
            if(args.Action == NotifyCollectionChangedAction.Add)
            {
                enterStatus = collision.ColliderA.Entity.Name + " entered " + collision.ColliderB.Entity.Name;
                exitStatus = "";
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                enterStatus = "";
                exitStatus = collision.ColliderA.Entity.Name + " exited " + collision.ColliderB.Entity.Name;
            }
        }

        public override void Update()
        {
            foreach (var collision in collider.Collisions)
            {
                // Entity A is the entity that collides with the current entity and triggers the collision
                // Entity B is the current entity that has a collider component
                DebugText.Print("ColliderA: " + collision.ColliderA.Entity.Name, new Int2(500, 300));
                DebugText.Print("ColliderB: " + collision.ColliderB.Entity.Name, new Int2(500, 320));
            }

            DebugText.Print(enterStatus, new Int2(200, 400));
            DebugText.Print(exitStatus, new Int2(700, 400));
        }
    }
}
