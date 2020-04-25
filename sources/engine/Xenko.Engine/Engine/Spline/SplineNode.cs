using Xenko.Core.Mathematics;

namespace Xenko.Engine.Spline
{
    public class SplineNode
    {
        private int _segments = 2;
        private int _nodesCount = 3;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private Vector3 p3;

        private Vector3 _startPos;
        private Vector3 _startHandlerPos;
        private Vector3 _targetHandlerPos;
        private Vector3 _targetPos;

        private SplinePointInfo[] _splinePoints;

        public float NodeLinkDistance { get; private set; } = 0;
        public float SplineDistance { get; private set; } = 0;

        public SplineNode(int segments, Vector3 startPos, Vector3 startHandlerPos, Vector3 targetPos, Vector3 targetHandlerPos)
        {
            _segments = segments;
            _nodesCount = _segments + 1;
            _startPos = startPos;
            _targetPos = targetPos;
            _startHandlerPos = startHandlerPos;
            _targetHandlerPos = targetHandlerPos;

            _splinePoints = new SplinePointInfo[_nodesCount];

            CalculateBezierSplineSpoints();
        }


        public SplinePointInfo[] GetSplinePoints()
        {
            return _splinePoints;
        }

        public struct SplinePointInfo
        {
            public Vector3 position;
            public Vector3 rotation;
            public float distance;
            public float totalDistance;
        }

        public void CalculateBezierSplineSpoints()
        {
            p0 = _startPos;
            p1 = _startHandlerPos;
            p2 = _targetHandlerPos;
            p3 = _targetPos;

            //var oriPivot = Pivot:Create();
            //var targetPivot = Pivot:Create();

            float t = 1.0f / _segments;
            for (var i = 0; i < _nodesCount; i++)
            {
                var p = CalculateBezierPoint(t * (i));
                _splinePoints[i].position = p;

                if (i > 0)
                {
                    //oriPivot.SetPosition(_splinePoints[i - 1], true);
                    //targetPivot.SetPosition(p, true);
                    //oriPivot.Point(targetPivot);

                    //todo fix rotation
                    //_splinePoints[i - 1].rotation = oriPivot:GetRotation(true);

                    if (i == _nodesCount)
                    {
                        _splinePoints[i].rotation = _splinePoints[i - 1].rotation;
                    }

                    var distance = Vector3.Distance(_splinePoints[i].position, _splinePoints[i - 1].position);
                    _splinePoints[i].distance = distance;
                    _splinePoints[i].totalDistance = _splinePoints[i - 1].totalDistance + distance;
                }
                else
                {
                    _splinePoints[i].distance = 0;
                    _splinePoints[i].totalDistance = 0;
                }
            }

            for (int i = 0; i < _nodesCount; i++)
            {
                SplineDistance += _splinePoints[i].distance;
            }

            NodeLinkDistance = Vector3.Distance(p0, p3);
        }

        private Vector3 CalculateBezierPoint(float t)
        {
            var tPower3 = t * t * t;
            var tPower2 = t * t;
            var oneMinusT = 1 - t;
            var oneMinusTPower3 = oneMinusT * oneMinusT * oneMinusT;
            var oneMinusTPower2 = oneMinusT * oneMinusT;
            var x = oneMinusTPower3 * p0.X + (3 * oneMinusTPower2 * t * p1.X) + (3 * oneMinusT * tPower2 * p2.X) + tPower3 * p3.X;
            var y = oneMinusTPower3 * p0.Y + (3 * oneMinusTPower2 * t * p1.Y) + (3 * oneMinusT * tPower2 * p2.Y) + tPower3 * p3.Y;
            var z = oneMinusTPower3 * p0.Z + (3 * oneMinusTPower2 * t * p1.Z) + (3 * oneMinusT * tPower2 * p2.Z) + tPower3 * p3.Z;
            return new Vector3(x, y, z);
        }

        public ClosestPointInfo GetClosestPointOnNodeCurve(Vector3 otherPosition)
        {
            //determine closest splinepoint
            var shortestSplinePointIndex = 1;
            float shortestSplinePointDistance = 0f;
            for (var i = 0; i < _nodesCount; i++)//do	
            {
                var curSplinePointDistance = Vector3.Distance(_splinePoints[i].position, otherPosition);

                if (curSplinePointDistance < shortestSplinePointDistance)
                {
                    shortestSplinePointDistance = curSplinePointDistance;
                    shortestSplinePointIndex = i;
                }
            }

            //determine previous or current splinepoint
            float shortestPreviousDistance = 0f;
            float shortestNextDistance = 0f;
            if (shortestSplinePointIndex - 1 > 0)
            {
                shortestPreviousDistance = Vector3.Distance(_splinePoints[shortestSplinePointIndex - 1].position, otherPosition);
            }

            if (shortestSplinePointIndex + 1 <= _splinePoints.Length)
            {
                shortestNextDistance = Vector3.Distance(_splinePoints[shortestSplinePointIndex + 1].position, otherPosition);
            }

            if (shortestPreviousDistance < shortestNextDistance)
            {
                shortestSplinePointIndex -= 1;
            }

            //Gather info
            var info = new ClosestPointInfo()
            {
                APosition = _splinePoints[shortestSplinePointIndex].position,
                AIndex = shortestSplinePointIndex
            };

            if (shortestSplinePointIndex + 1 <= _splinePoints.Length)
            {
                info.BPosition = _splinePoints[shortestSplinePointIndex + 1].position;
                info.BIndex = shortestSplinePointIndex + 1;
            }
            
            if (info.BPosition != null)
            {
                info.ClosestPoint = ProjectPointOnLineSegment(info.APosition, info.BPosition, otherPosition);
            }
            else
            {
                info.ClosestPoint = info.APosition;
            }

            return info;
        }

        public struct ClosestPointInfo
        {
            public Vector3 ClosestPoint;
            public Vector3 APosition;
            public int AIndex;
            public Vector3 BPosition;
            public int BIndex;
        }

        public Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
        {
            var vector = linePoint2 - linePoint1;
            vector.Normalize();
            var projectedPoint = ProjectPointOnLine(linePoint1, vector, point);
            var side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

            if (side == 0)
            {
                return projectedPoint;
            }


            if (side == 1)
            {

                return linePoint1;
            }


            //if (side == 2)
            {

                return linePoint2;
            }
        }

        public Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
        {
            var t = Vector3.Dot(point, linePoint);
            return linePoint + lineVec * t;
        }

        public int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
        {
            var lineVec = linePoint2 - linePoint1;
            var pointVec = point - linePoint1;
            var dot = Vector3.Dot(linePoint1, linePoint2);

            if (dot > 0)
            {

                if (pointVec.Length() * 2 <= lineVec.Length() * 2)
                {
                    return 0;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                return 1;
            }
        }
    }
}
