// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Engine
{
    /// <summary>
    /// Updates <see cref="Engine.SplineComponent"/>.
    /// </summary>
    public class SplineViewHierarchyTransformOperation : TransformOperation
    {
        public readonly SplineComponent SplineComponent;

        public SplineViewHierarchyTransformOperation(SplineComponent modelComponent)
        {
            SplineComponent = modelComponent;
        }

        /// <inheritdoc/>
        public override void Process(TransformComponent transformComponent)
        {
            SplineComponent.Update(transformComponent);
        }
    }
}
