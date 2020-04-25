//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Engine.Processors
{
    /// <summary>
    /// The processor for <see cref="SplineFollowerComponent"/>.
    /// </summary>
    public class SplineFollowerTransformProcessor : EntityProcessor<SplineFollowerComponent, SplineFollowerTransformProcessor.SplineFollowerTransformationInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SplineTransformProcessor"/> class.
        /// </summary>
        public SplineFollowerTransformProcessor()
            : base(typeof(TransformComponent))
        {
        }

        protected override SplineFollowerTransformationInfo GenerateComponentData(Entity entity, SplineFollowerComponent component)
        {
            return new SplineFollowerTransformationInfo
            {
                TransformOperation = new SplineFollowerViewHierarchyTransformOperation(component),
            };
        }

        protected override bool IsAssociatedDataValid(Entity entity, SplineFollowerComponent component, SplineFollowerTransformationInfo associatedData)
        {
            return component == associatedData.TransformOperation.SplineFollowerComponent;
        }

        protected override void OnEntityComponentAdding(Entity entity, SplineFollowerComponent component, SplineFollowerTransformationInfo data)
        {
            // Register model view hierarchy update
            entity.Transform.PostOperations.Add(data.TransformOperation);
        }

        protected override void OnEntityComponentRemoved(Entity entity, SplineFollowerComponent component, SplineFollowerTransformationInfo data)
        {
            // Unregister model view hierarchy update
            entity.Transform.PostOperations.Remove(data.TransformOperation);
        }

        public class SplineFollowerTransformationInfo
        {
            public SplineFollowerViewHierarchyTransformOperation TransformOperation;
        }
    }
}
