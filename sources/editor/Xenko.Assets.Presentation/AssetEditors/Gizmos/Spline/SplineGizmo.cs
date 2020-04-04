// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Assets.Presentation.AssetEditors.Gizmos.Spline.Mesh;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo that displays connected spline nodes
    /// </summary>
    [GizmoComponent(typeof(SplineComponent), false)]
    public class SplineGizmo : BillboardingGizmo<SplineComponent>
    {
        private Entity mainGizmoEntity;
        private Entity gizmoNodes;
        private Entity gizmoNodeLinks;
        private Entity gizmoSegments;
        private Entity gizmoPoints;
        private Entity gizmoTangentOut;
        private Entity gizmoTangentIn;
        private float updateFrequency = 1.2f;
        private float updateTimer = 0.0f;

        public SplineGizmo(EntityComponent component) : base(component, "Spline gizmo", GizmoResources.SplineGizmo)
        {
        }

        protected override Entity Create()
        {
            mainGizmoEntity = new Entity();
            gizmoNodes = new Entity();
            gizmoNodeLinks = new Entity();
            gizmoSegments = new Entity();
            gizmoPoints = new Entity();
            gizmoTangentOut = new Entity();
            gizmoTangentIn = new Entity();
            mainGizmoEntity.AddChild(gizmoNodes);
            mainGizmoEntity.AddChild(gizmoNodeLinks);
            mainGizmoEntity.AddChild(gizmoSegments);
            mainGizmoEntity.AddChild(gizmoPoints);
            mainGizmoEntity.AddChild(gizmoTangentOut);
            mainGizmoEntity.AddChild(gizmoTangentIn);
            return mainGizmoEntity;
        }

        public override void Update()
        {
            updateTimer += (float)Game.UpdateTime.Elapsed.TotalSeconds;

            if (ContentEntity == null || GizmoRootEntity == null)
                return;

            if (updateTimer > updateFrequency)
            {
                updateTimer = 0;
                return;
            }

            // calculate the world matrix of the gizmo so that it is positioned exactly as the corresponding scene entity
            // except the scale that is re-adjusted to the gizmo desired size (gizmo are insert at scene root so LocalMatrix = WorldMatrix)
            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            ContentEntity.Transform.WorldMatrix.Decompose(out scale, out rotation, out translation);

            // Translation and Scale but no rotation on bounding boxes
            GizmoRootEntity.Transform.Position = translation;
            GizmoRootEntity.Transform.Scale = 1 * scale;
            GizmoRootEntity.Transform.UpdateWorldMatrix();

            if (Component.Nodes?.Count > 1)
            {
                ClearChildren(gizmoNodes);
                ClearChildren(gizmoNodeLinks);
                ClearChildren(gizmoSegments);
                ClearChildren(gizmoPoints);
                ClearChildren(gizmoTangentOut);
                ClearChildren(gizmoTangentIn);

                foreach (var node in Component.Nodes)
                {
                    if (node == null || !node.Dirty)
                    {
                        break;
                    }

                    if (Component.DebugInfo.Nodes)
                    {
                        DrawNodes(node);
                    }


                    if (node.Next == null)
                    {
                        break;
                    }
                    node.MakeClean();


                    if (Component.DebugInfo.NodesLink)
                    {
                        DrawNodeLinks(node);
                    }

                    if (Component.DebugInfo.Segments || Component.DebugInfo.Points)
                    {
                        var splinePointsInfo = node.GetSplineNode().GetSplinePointInfo();
                        var splinePoints = new Vector3[splinePointsInfo.Length];
                        for (int i = 0; i < splinePointsInfo.Length; i++)
                        {
                            splinePoints[i] = splinePointsInfo[i].position;
                        }

                        if (Component.DebugInfo.Points)
                        {
                            DrawSplinePoints(splinePoints);
                        }

                        if (Component.DebugInfo.Segments)
                        {
                            DrawSplineSegments(splinePoints);
                        }
                    }

                    if (Component.DebugInfo.TangentOutwards)
                    {
                        DrawTangentOutwards(node);
                    }

                    if (Component.DebugInfo.TangentInwards)
                    {
                        DrawTangentInwards(node);
                    }
                }
            }
        }

        private void DrawSplineSegments(Vector3[] splinePoints)
        {
            var localPoints = new Vector3[splinePoints.Length];
            for (int i = 0; i < splinePoints.Length; i++)
            {
                localPoints[i] = mainGizmoEntity.Transform.WorldToLocal(splinePoints[i]);
            }

            for (int i = 0; i < localPoints.Length - 1; i++)
            {
                var lineMesh = new LineMesh(GraphicsDevice);
                lineMesh.Build(new Vector3[2] { localPoints[i], localPoints[i + 1]- localPoints[i] });

                var segment = new Entity()
                {
                    new ModelComponent
                    {
                        Model = new Model
                        {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, i % 2 == 0 ? Color.Orange: Color.OrangeRed),
                            new Mesh { Draw = lineMesh.MeshDraw }
                        },
                        RenderGroup = RenderGroup,
                    }
                };

                gizmoSegments.AddChild(segment);
                segment.Transform.Position += localPoints[i];
            }
        }

        private void DrawSplinePoints(Vector3[] splinePoints)
        {
            for (int i = 0; i < splinePoints.Length; i++)
            {
                var pointMesh = new BulbMesh(GraphicsDevice, 0.2f);
                pointMesh.Build();

                var point = new Entity()
                {
                    new ModelComponent
                    {
                        Model = new Model
                        {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, Color.PeachPuff),
                            new Mesh { Draw = pointMesh.MeshDraw }
                        },
                        RenderGroup = RenderGroup,
                    }
                };

                point.Transform.Position = mainGizmoEntity.Transform.WorldToLocal(splinePoints[i]);
                gizmoPoints.AddChild(point);
            }
        }


        private void DrawNodes(SplineNodeComponent splineNodeComponent)
        {
            var nodeMesh = new BulbMesh(GraphicsDevice, 0.4f);
            nodeMesh.Build();

            var node = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightSkyBlue),
                        new Mesh { Draw = nodeMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoNodes.AddChild(node);
            node.Transform.Position += splineNodeComponent.Entity.Transform.Position;
        }

        private void DrawNodeLinks(SplineNodeComponent splineNodeComponent)
        {
            var nodeLinkLineMesh = new LineMesh(GraphicsDevice);
            nodeLinkLineMesh.Build(new Vector3[2] { splineNodeComponent.Entity.Transform.Position, splineNodeComponent.Next.Entity.Transform.Position - splineNodeComponent.Entity.Transform.Position });
            var nodeLink = new Entity()
                {
                    new ModelComponent
                    {
                        Model = new Model
                        {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green),
                            new Mesh { Draw = nodeLinkLineMesh.MeshDraw }
                        },
                        RenderGroup = RenderGroup
                    }
                };
            gizmoNodeLinks.AddChild(nodeLink);
            nodeLink.Transform.Position += splineNodeComponent.Entity.Transform.Position;
        }

        private void DrawTangentOutwards(SplineNodeComponent splineNodeComponent)
        {
            var outMesh = new BulbMesh(GraphicsDevice, 0.3f);
            outMesh.Build();

            var outEntity = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightGray),
                        new Mesh { Draw = outMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoTangentOut.AddChild(outEntity);
            outEntity.Transform.Position += splineNodeComponent.TangentOut;
        }

        private void DrawTangentInwards(SplineNodeComponent splineNodeComponent)
        { 
            var inMesh = new BulbMesh(GraphicsDevice, 0.3f);
            inMesh.Build();

            var inEntity = new Entity()
            {
                new ModelComponent
                {
                    Model = new Model
                    {
                        GizmoUniformColorMaterial.Create(GraphicsDevice, Color.HotPink),
                        new Mesh { Draw = inMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            };

            gizmoTangentOut.AddChild(inEntity);
            inEntity.Transform.Position += splineNodeComponent.TangentIn;
        }

        private void ClearChildren(Entity entity)
        {
            var children = entity.GetChildren();
            foreach (var child in children)
            {
                entity.RemoveChild(child);
            }
        }
    }
}
