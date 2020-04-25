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
    [Display("Spline node", Expand = ExpandRule.Once)]
    [ComponentCategory("Splines")]
    public sealed class SplineNodeComponent : EntityComponent
    {
        public delegate void SplineNodeDirtyEventHandler();
        public event SplineNodeDirtyEventHandler OnDirty;

        //#region NextNode
        //private SplineNodeComponent _next;
        //[DataMemberIgnore]
        //public SplineNodeComponent Next
        //{
        //    get { return _next; }
        //    set
        //    {
        //        _next = value;

        //        if (_next != null)
        //        {
        //            OnDirty?.Invoke();
        //            _next.Previous = this;
        //        }
        //    }
        //}
        //#endregion

        //#region PreviousNode
        //private SplineNodeComponent _previous;
        //[DataMemberIgnore]
        //public SplineNodeComponent Previous
        //{
        //    get { return _previous; }
        //    set
        //    {
        //        _previous = value;
        //        OnDirty?.Invoke();
        //    }
        //}
        //#endregion

        #region Out
        private Vector3 _tangentOut { get; set; }
        public Vector3 TangentOut
        {
            get { return _tangentOut; }
            set
            {
                _tangentOut = value;
                OnDirty?.Invoke();

            }
        }
        #endregion

        #region In
        private Vector3 _TangentIn { get; set; }
        public Vector3 TangentIn

        {
            get { return _TangentIn; }
            set
            {
                _TangentIn = value;
                OnDirty?.Invoke();

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
                OnDirty?.Invoke();

            }
        }
        #endregion

        private SplineNode _splineNode;
        private Vector3 _previousVector;

        internal void Update(TransformComponent transformComponent)
        {
            CheckDirtyness();

            _previousVector = Entity.Transform.Position;
        }

        private void CheckDirtyness()
        {
            if (_previousVector.X != Entity.Transform.Position.X ||
                    _previousVector.Y != Entity.Transform.Position.Y ||
                    _previousVector.Z != Entity.Transform.Position.Z)
            {
                OnDirty?.Invoke();
            }
        }

        public void MakeDirty()
        {
            OnDirty?.Invoke();

        }

        public void UpdateBezierCurve(SplineNodeComponent nextNode)
        {
            if (nextNode != null)
            {
                Vector3 scale;
                Quaternion rotation;
                Vector3 entityWorldPos;
                Vector3 nextWorldPos;

                Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out entityWorldPos);
                nextNode.Entity.Transform.WorldMatrix.Decompose(out scale, out rotation, out nextWorldPos);
                Vector3 TangentOutWorld = entityWorldPos + TangentOut;
                Vector3 TangentInWorld = nextWorldPos + nextNode.TangentIn;

                _splineNode = new SplineNode(Segments, entityWorldPos, TangentOutWorld, nextWorldPos, TangentInWorld);
            }
        }

        public SplineNode GetSplineNode()
        {
            return _splineNode;
        }
    }
}
