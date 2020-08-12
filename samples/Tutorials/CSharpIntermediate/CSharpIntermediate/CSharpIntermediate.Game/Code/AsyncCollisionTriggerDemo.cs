using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class AsyncCollisionTriggerDemo : AsyncScript
    {
        PhysicsComponent triggerCollider;
        Collision firstCollision;
        string enterStatus = "";
        string exitStatus = "";

        public override async Task Execute()
        {
            triggerCollider = Entity.Get<PhysicsComponent>();
            //triggerCollider.ProcessCollisions = true;
            
            
            // 1. Wait for an entity to collide with the trigger
            firstCollision = await triggerCollider.NewCollision();

            var ballCollider = triggerCollider == firstCollision.ColliderA
                ? firstCollision.ColliderB
                : firstCollision.ColliderA;

            enterStatus = ballCollider.Entity.Name + " entered " + triggerCollider.Entity.Name;
            exitStatus = "";

            DebugText.Print("ColliderA: " + firstCollision.ColliderA.Entity.Name, new Int2(500, 300));
            DebugText.Print("ColliderB: " + firstCollision.ColliderB.Entity.Name, new Int2(500, 320));


            // 2. Wait for the entity to exit the trigger
            await firstCollision.Ended();
            enterStatus = "";
            exitStatus = ballCollider.Entity.Name + " left " + triggerCollider.Entity.Name;

            //async Task collisionEndTask()
            //{
            //    Collision collision;
            //    do
            //    {
            //        DebugText.Print(enterStatus, new Int2(200, 400));
            //        DebugText.Print(exitStatus, new Int2(700, 400));
            //        collision = await triggerCollider.CollisionEnded();
                   
            //    }
            //    while (collision != firstCollision);
            //}

            //Script.AddTask(collisionEndTask);
        }
    }
}
