using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core.Mathematics;

namespace Xenko.Engine.Spline
{
    public class SplineNode
    {
        private int _segments = 2;
        private Vector3 p0;
        private Vector3 p1;
        private Vector3 p2;
        private Vector3 p3;

        private Vector3 _startPos;
        private Vector3 _startHandlerPos;
        private Vector3 _targetHandlerPos;
        private Vector3 _targetPos;

        public SplineNode(int segments, Vector3 startPos, Vector3 startHandlerPos, Vector3 targetPos, Vector3 targetHandlerPos)
        {
            _segments = 2;
            _startPos = startPos;
            _targetPos = targetPos;
            _startHandlerPos = startHandlerPos;
            _targetHandlerPos = targetHandlerPos;
        }

        public void CreateBezierSpline()
        {
            p0 = _startPos;
            p1 = _startHandlerPos;
            p2 = _targetHandlerPos;
            p3 = _targetPos;

            var oriPivot = Pivot:Create();
            var targetPivot = Pivot:Create();

            float t = 1.0f / _segments;
            for (var i = 0; i < _segments; i++)
            {
                var p = CalculateBezierPoint(t * (i - 1));



                splinePoints[i] = { };
                splinePoints[i].point = p;
                splinePoints[i].color = Vec4(Math: Random(0, 1), Math: Random(0, 1), Math: Random(0, 1), 1);

                if (i > 1)
                {
oriPivot: SetPosition(splinePoints[i - 1].point, true);

targetPivot: SetPosition(p, true);
oriPivot: Point(targetPivot);



                    splinePoints[i - 1].rotation = oriPivot:GetRotation(true);



                    if (i == _segments + 1)
                    {
                        splinePoints[i].rotation = splinePoints[i - 1].rotation;
                    }


                    var distance = splinePoints[i].point:DistanceToPoint(splinePoints[i - 1].point);



                    splinePoints[i].distance = distance;
                    splinePoints[i].totalDistance = splinePoints[i - 1].totalDistance + distance;

        else
                    {
                        splinePoints[i].distance = 0;
                        splinePoints[i].totalDistance = 0;

                    }
                }
                for key, splinePoint in pairs(splinePoints)
                {
                    splineLength = splineLength + splinePoint.distance;
                }

                lineLength = p0.DistanceToPoint(p3);
                return splineLength;
            }

            private Vector3 CalculateBezierPoint(float t)
            {
                var tPower3 = t * t * t;
                var tPower2 = t * t;
                var oneMinusT = 1 - t;
                var oneMinusTPower3 = oneMinusT * oneMinusT * oneMinusT;
                var oneMinusTPower2 = oneMinusT * oneMinusT;
                var x = oneMinusTPower3 * p0.x + (3 * oneMinusTPower2 * t * p1.x) + (3 * oneMinusT * tPower2 * p2.x) + tPower3 * p3.x;
                var y = oneMinusTPower3 * p0.y + (3 * oneMinusTPower2 * t * p1.y) + (3 * oneMinusT * tPower2 * p2.y) + tPower3 * p3.y;
                var z = oneMinusTPower3 * p0.z + (3 * oneMinusTPower2 * t * p1.z) + (3 * oneMinusT * tPower2 * p2.z) + tPower3 * p3.z;
                return new Vector3(x, y, z);
            }

            public Vector3 GetClosestPointOnNodeCurve(Vector3 otherPosition)
            {
                //determine closest splinepoint
                var shortestSplinePointIndex = 1;
                var shortestSplinePointDistance = 0;
                for (var i = 0; i < splinePoints.Length(); i++)//do	
                {
                    var curSplinePointDistance = splinePoints[i].point.DistanceToPoint(otherPosition)
    


            if (shortestSplinePointDistance == null || curSplinePointDistance < shortestSplinePointDistance)
                    {
                        shortestSplinePointDistance = curSplinePointDistance;
                        shortestSplinePointIndex = i;
                    }

                }



                //determine previous or current splinepoint
                var shortestPreviousDistance = null;
                var shortestNextDistance = null;

                if (shortestSplinePointIndex - 1 > 0)
                {
                    shortestPreviousDistance = splinePoints[shortestSplinePointIndex - 1].point:DistanceToPoint(otherPosition);

        }


            if (shortestSplinePointIndex + 1 <= splinePoints.Length())
            {
                shortestNextDistance = splinePoints[shortestSplinePointIndex + 1].point.DistanceToPoint(otherPosition);


            }


            if (shortestPreviousDistance != null && (shortestNextDistance == null || shortestPreviousDistance<shortestNextDistance) {
                shortestSplinePointIndex = shortestSplinePointIndex - 1;


            }

//Gather info
var B = null;
var BIndex = null;
            if shortestSplinePointIndex + 1 <= splinePoints.Length())
            {
                BIndex = shortestSplinePointIndex + 1;
                B = splinePoints[shortestSplinePointIndex + 1].point;
            }

            var info = {
                currentNodeEntity = entity,
        A = splinePoints[shortestSplinePointIndex].point,
        AIndex = shortestSplinePointIndex,
        B = B,
        BIndex = BIndex
    }


            var closestPoint;

            if (info.B != null)
            {

                closestPoint = ProjectPointOnLineSegment(info.A, info.B, otherPosition);

            }
            else
            {
                closestPoint = info.A;
            }

            info["closestPoint"] = closestPoint;

            return info;
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
