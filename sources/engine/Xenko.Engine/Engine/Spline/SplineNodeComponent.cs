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
    [Display("Spline node")]
    [ComponentCategory("Splines")]
    public sealed class SplineNodeComponent : EntityComponent
    {
        public SplineDebugInfo Info;

        #region NextNode
        private SplineNodeComponent _next;
        public SplineNodeComponent Next
        {
            get { return _next; }
            set
            {
                _next = value;

                if (_next != null)
                {
                    Info.IsDirty = true;
                    _next.Previous = this;
                }
            }
        }
        #endregion

        #region PreviousNode
        private SplineNodeComponent _previous;
        [DataMemberIgnore]
        public SplineNodeComponent Previous
        {
            get { return _previous; }
            set
            {
                _previous = value;
                Info.IsDirty = true;
            }
        }
        #endregion

        #region Out
        private Vector3 _outHandler { get; set; }
        public Vector3 OutHandler
        {
            get { return _outHandler; }
            set
            {
                _outHandler = value;
                Info.IsDirty = true;
            }
        }
        #endregion

        #region In
        private Vector3 _inHandler { get; set; }
        public Vector3 InHandler

        {
            get { return _inHandler; }
            set
            {
                _inHandler = value;
                Info.IsDirty = true;
            }
        }
        #endregion

        #region Segments
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
                Info.IsDirty = true;
            }
        }
        #endregion

        private SplineNode _splineNode;
        private Vector3 _previousVector;

        internal void Update(TransformComponent transformComponent)
        {
            CheckDirtyness();

            if (Next != null && Info.IsDirty)
            {
                UpdateBezierCurve();
            }

            if (Previous != null && Info.IsDirty)
            {
                Previous.MakeDirty();
            }

            _previousVector = Entity.Transform.Position;
        }

        private void CheckDirtyness()
        {
            if (_previousVector.X != Entity.Transform.Position.X ||
                    _previousVector.Y != Entity.Transform.Position.Y ||
                    _previousVector.Z != Entity.Transform.Position.Z)
            {
                Info.IsDirty = true;
            }
        }

        public void MakeDirty()
        {
            Info.IsDirty = true;
        }

        public void UpdateBezierCurve()
        {
            if (Next != null)
            {
                Vector3 scale;
                Quaternion rotation;
                Vector3 entityWorldPos;
                Vector3 nextWorldPos;
                Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out entityWorldPos);
                Next.Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out nextWorldPos);
                _splineNode = new SplineNode(Segments, entityWorldPos, OutHandler, nextWorldPos, Next.InHandler);
            }
        }

        public SplineNode GetSplineNode()
        {
            if (_splineNode == null && Next != null)
            {
                UpdateBezierCurve();
            }

            return _splineNode;
        }
    }
}
