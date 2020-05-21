using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class AsyncCollisionTrigger : AsyncScript
    {
        PhysicsComponent trigger;
        Collision firstCollision;

        public override async Task Execute()
        {
            trigger = Entity.Get<PhysicsComponent>();
            trigger.ProcessCollisions = true;

            firstCollision = await trigger.NewCollision();

            Func<Task> collisionEndTask = async () =>
            {
                Collision collision;
                do
                {
                    collision = await trigger.CollisionEnded();
                } while (collision != firstCollision);
            };

            Script.AddTask(collisionEndTask);
        }
    }
}
