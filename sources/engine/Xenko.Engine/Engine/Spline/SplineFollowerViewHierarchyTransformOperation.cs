//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Engine
{
    /// <summary>
    /// Updates <see cref="Engine.SplineFollowerComponent"/>.
    /// </summary>
    public class SplineFollowerViewHierarchyTransformOperation : TransformOperation
    {
        public readonly SplineFollowerComponent SplineFollowerComponent;

        public SplineFollowerViewHierarchyTransformOperation(SplineFollowerComponent modelComponent)
        {
            SplineFollowerComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            SplineFollowerComponent.Update(transformComponent);
        }
    }
}
