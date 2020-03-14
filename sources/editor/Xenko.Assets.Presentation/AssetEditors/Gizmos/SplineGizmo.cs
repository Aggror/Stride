// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Navigation;
using Xenko.Rendering;
using Xenko.Rendering.Lights;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo to display the bounding boxes for navigation meshes inside the editor as a gizmo. 
    /// this gizmo uses scale as the extent of the bounding box and is not affected by rotation
    /// </summary>
    [GizmoComponent(typeof(SplineNodeComponent), false)]
    public class SplineGizmo : EntityGizmo<SplineNodeComponent>
    {
        private Material material;
        private Entity debugEntity;
        private Entity debugEntityBezier;

        public SplineGizmo(EntityComponent component) : base(component)
        {
        }

        protected override Entity Create()
        {
            debugEntity = new Entity($"Bezier node link {Component.Entity.Name}");
            debugEntityBezier = new Entity($"bezier bezier curve line {Component.Entity.Name}");
            debugEntity.AddChild(debugEntityBezier);
            return debugEntity;
        }

        public override void Update()
        {
            if (ContentEntity == null || GizmoRootEntity == null)
                return;

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

            if (Component.Next != null && debugEntity != null)
            {
                //var children = debugEntity.GetChildren();
                //foreach (var child in children)
                //{
                //    debugEntity.RemoveChild(child);
                //}

                var color = new Color[7];
                color[0] = Color.Red;
                color[1] = Color.Green;
                color[2] = Color.Yellow;
                color[3] = Color.Purple;
                color[4] = Color.Blue;
                color[5] = Color.Orange;
                color[6] = Color.Pink;

                if (Component.DebugNodesLink)
                {
                    var nodeLinkLineMesh = new LineMesh(GraphicsDevice);
                    nodeLinkLineMesh.Build(new Vector3[2] { Component.Entity.Transform.Position, Component.Next.Entity.Transform.Position - Component.Entity.Transform.Position });
                    debugEntity.RemoveAll<ModelComponent>();
                    debugEntity.Add(
                        new ModelComponent
                        {
                            Model = new Model
                            {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, color[6]),
                            new Mesh { Draw = nodeLinkLineMesh.MeshDraw }
                            },
                            RenderGroup = RenderGroup
                        }
                    );
                }

                if (Component.DebugSegments)
                {
                    var splinePointsInfo = Component.GetSplineNode().GetSplinePointInfo();

                    var splinePoints = new Vector3[splinePointsInfo.Length];
                    for (int i = 0; i < splinePointsInfo.Length; i++)
                    {
                        splinePoints[i] = splinePointsInfo[i].position;
                    }
                    var bezierCurveMesh = new LineMesh(GraphicsDevice);
                    bezierCurveMesh.Build(splinePoints);
                    debugEntityBezier.RemoveAll<ModelComponent>();
                    debugEntityBezier.Add(
                        new ModelComponent
                        {
                            Model = new Model
                            {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, color[5]),
                            new Mesh { Draw = bezierCurveMesh.MeshDraw }
                            },
                            RenderGroup = RenderGroup
                        }
                    );
                }
            }
        }

        class LineMesh
        {
            public MeshDraw MeshDraw;

            private Buffer vertexBuffer;

            private readonly GraphicsDevice graphicsDevice;

            public LineMesh(GraphicsDevice graphicsDevice)
            {
                this.graphicsDevice = graphicsDevice;
            }

            public void Build(Vector3[] positions)
            {
                var indices = new int[12 * 2];
                var vertices = new VertexPositionNormalTexture[positions.Length];

                for (int i = 0; i < positions.Length; i++)
                {
                    vertices[0] = new VertexPositionNormalTexture(positions[i], Vector3.UnitY, Vector2.Zero);
                }

                int indexOffset = 0;
                // Top sides
                for (int i = 0; i < 4; i++)
                {
                    indices[indexOffset++] = i;
                    indices[indexOffset++] = (i + 1) % 4;
                }

                vertexBuffer = Buffer.Vertex.New(graphicsDevice, vertices);
                MeshDraw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.LineList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertexBuffer.ElementCount) },
                };
            }
        }
    }
}
