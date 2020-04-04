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
        public SplineDebugInfo DebugInfo;
        private Scene _editorScene;

        private List<SplineNodeComponent> _nodes;
        public List<SplineNodeComponent> Nodes
        {
            get
            {

                if (_nodes == null)
                {
                    _nodes = new List<SplineNodeComponent>();
                }
                return _nodes;
            }
            set
            {
                _nodes = value;
                DebugInfo.IsDirty = true;
            }
        }

        public SplineComponent()
        {
            //CreateSplineNodeEntity();
            //CreateSplineNodeEntity();
        }

        internal void Initialize()
        {
            //_editorScene = editorScene;

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
        }

        //Button to create new spline node
        private bool _createSplineNode;
        public bool CreateSplineNode
        {
            get
            {

                return _createSplineNode;
            }
            set
            {
                _createSplineNode = value;
                if (_createSplineNode)
                {
                    CreateSplineNodeEntity();
                }
            }
        }

        public void CreateSplineNodeEntity()
        {
            if (_nodes == null)
            {
                _nodes = new List<SplineNodeComponent>();
            }

            var nodesCount = Nodes.Count;
            var entityName = "Node_" + nodesCount;
            var startPos = nodesCount > 0 ? Nodes[nodesCount - 1].Entity.Transform.Position : Entity.Transform.Position;
            var newSplineNode = new Entity(startPos, entityName);
            var newSplineNodeComponent = newSplineNode.GetOrCreate<SplineNodeComponent>();
            Nodes.Add(newSplineNodeComponent);
            _editorScene?.Entities.Add(newSplineNode);
            //Entity.Scene.Entities.Add(entity);
        }
    }
}
