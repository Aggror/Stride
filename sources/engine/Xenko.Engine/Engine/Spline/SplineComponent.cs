//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;
using Xenko.Engine.Spline;

namespace Xenko.Engine
{
    /// <summary>
    /// Component representing an Spline.
    /// </summary>
    [DataContract("SplineComponent")]
    [Display("Spline", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(SplineTransformProcessor))]
    [ComponentCategory("Splines")]
    public sealed class SplineComponent : EntityComponent
    {
        private List<SplineNodeComponent> _nodes;
        public SplineDebugInfo DebugInfo;
        public bool CreateSplineNode;


        public SplineComponent()
        {
            //CreateSplineNodeEntity();
            //CreateSplineNodeEntity();
        }

        internal void Initialize()
        {
        }

        ///// <summary>
        ///// Creates a new spline node
        ///// </summary>
        //private bool createNode;
        //[DataMember(30)]
        //[DefaultValue(true)]
        //[Display("Create node")]
        //public bool CreateNode {
        //    get { return createNode; }
        //    set
        //    {
        //        CreateSplineNodeEntity();
        //    }
        //}


        internal void Update(TransformComponent transformComponent)
        {
            Console.WriteLine(Entity.Name);
            if (CreateSplineNode)
            {
                CreateSplineNodeEntity();
                CreateSplineNode = false;
            }
        }

        public void CreateSplineNodeEntity()
        {
            if(_nodes == null)
            {
                _nodes = new List<SplineNodeComponent>();
            }

            var nodesCount = _nodes.Count;
            var entityName = "Node_" + nodesCount;
            var startPos = nodesCount > 0 ? _nodes[nodesCount - 1].Entity.Transform.Position : Entity.Transform.Position;
            var entity = new Entity(startPos, entityName);

            var newSplineNodeComponent = entity.GetOrCreate<SplineNodeComponent>();
            entity.Scene.Entities.Add(entity);
            //Nodes.Add(newSplineNodeComponent);
        }
    }
}
