// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;
using Xenko.Engine.Spline;

namespace Xenko.Engine
{
    /// <summary>
    /// Component representing a  Spline node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Associate this component to an entity to maintain bezier curves that create a spline.
    /// </para>
    /// </remarks>

    [DataContract]
    [DefaultEntityComponentProcessor(typeof(SplineNodeTransformProcessor), ExecutionMode = ExecutionMode.All)]
    [Display("spline node")]
    [ComponentCategory("Splines")]
    public sealed class SplineNodeComponent : EntityComponent
    {
        public SplineNodeComponent Next { get; set; }
        public SplineNodeComponent Previous { get; set; }

        public Vector3 OutHandler { get; set; }
        public Vector3 InHandler { get; set; }

        private int _segments = 2;
        public int Segments
        {
            get { return _segments; }
            set
            {
                if (value < 2)
                {
                    _segments = 2;
                }
                else
                {
                    _segments = value;
                }
            }
        }

        public bool DebugSegments { get; set; }
        public bool DebugNodesLink { get; set; }

        private SplineNode _splineNode;
        private bool _calculateSubNodes = true;


        internal void Update(TransformComponent transformComponent)
        {
             CalculateBezierCurve();
            
            if (Previous != null)
            {
                Previous.UpdateBezierCurve();
            }  
        }

        private void CalculateBezierCurve()
        {
            if (Next != null && _calculateSubNodes)
            {
                _splineNode = new SplineNode(Segments, Entity.Transform.Position, OutHandler, Next.Entity.Transform.Position, Next.InHandler);
                _splineNode.CalculateBezierSplineSpoints();
            }
        }

        public void UpdateBezierCurve()
        {
            if (Next != null)
            {
                Previous.UpdateBezierCurve();
            }
        }

        public SplineNode GetSplineNode()
        {
            if(_splineNode == null)
            {
                CalculateBezierCurve();
            }

            return _splineNode;
        }
    }
}
