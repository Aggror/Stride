//// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
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

        public bool Dirty { get; set; }

        private int _previousNodeCount = 0;
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
            }
        }

        public SplineComponent()
        {
            _previousNodeCount = 0;
            Nodes = new List<SplineNodeComponent>();
        }

        internal void Initialize()
        {
            UpdateSpline();
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
            int currentNodeCount = Nodes.Count;
            if (_previousNodeCount != currentNodeCount)
            {
                DeregisterSplineNodeDirtyEvents();
                UpdateSpline();
            }
            else
            {
                if (Dirty)
                {
                    UpdateSpline();
                }
            }

            _previousNodeCount = currentNodeCount;
        }

        private void DeregisterSplineNodeDirtyEvents()
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                var curNode = Nodes[i];
                curNode.OnDirty -= MakeSplineDirty;
            }
        }

        public void UpdateSpline()
        {
            Dirty = true;
            if (Nodes.Count > 1)
            {
                var totalNodesCount = Nodes.Count;
                for (int i = 0; i < totalNodesCount; i++)
                {
                    var curNode = Nodes[i];
                    if (i < totalNodesCount - 1)
                        curNode?.UpdateBezierCurve(Nodes[i + 1]);

                    curNode.OnDirty += MakeSplineDirty;
                }
            }
        }

        private void MakeSplineDirty()
        {
            Dirty = true;
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
            var nodesCount = Nodes.Count;
            var entityName = "Node_" + nodesCount;
            var startPos = nodesCount > 0 ? Nodes[nodesCount - 1].Entity.Transform.Position : Entity.Transform.Position;
            var newSplineNode = new Entity(startPos, entityName);
            var newSplineNodeComponent = newSplineNode.GetOrCreate<SplineNodeComponent>();
            Nodes.Add(newSplineNodeComponent);
            //SceneSystem.SceneInstance.RootScene.Entities.Add(newSplineNode);
            //_editorScene?.Entities.Add(newSplineNode);

            //Entity.Scene.Entities.Add(entity);
        }

        public float GetTotalSplineDistance()
        {
            float distance = 0;
            foreach (var node in Nodes)
            {
                distance += node.GetSplineNode().SplineDistance;
            }
            return distance;
        }

        public float GetNodeLinkDistance()
        {
            float distance = 0;
            foreach (var node in Nodes)
            {
                distance += node.GetSplineNode().NodeLinkDistance;
            }
            return distance;
        }

        public ClosestPointInfo GetClosestPointOnSpline(Vector3 otherPosition)
        {
            ClosestPointInfo closestPointInfo = null;

            var totalNodesCount = Nodes.Count;
            for (int i = 0; i < totalNodesCount-1; i++)
            {

                //for key, nodeEntity in ipairs(self.nodeEntities) do
                //        local nextNodeEntity = nodeEntity.script.nextNodeEntity
                //    if nextNodeEntity ~= nil then
                //        local closestPointInfoTemp = nodeEntity.script.node:GetClosestPointOnNodeCurve(otherPosition)
                //        local dist = closestPointInfoTemp.closestPoint:DistanceToPoint(otherPosition)

                //        if shortestDistance == nil or shortestDistance > dist then

                //            shortestDistance = dist
                //            closestPointInfo = closestPointInfoTemp
                //        end
                //    else
                //    break
                //    end
                //end
                //closestPointInfo.distance = closestPointInfo.closestPoint:DistanceToPoint(otherPosition)
                //closestPointInfo.Distance = closestPointInfo.ClosestPoint:
            }

            return closestPointInfo;
        }

        public class ClosestPointInfo
        {
            public float Distance = 0;
            public Vector3 ClosestPoint;
        }
    }
}
