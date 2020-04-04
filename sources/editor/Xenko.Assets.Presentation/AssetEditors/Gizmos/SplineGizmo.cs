// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;
using Buffer = Xenko.Graphics.Buffer;

namespace Xenko.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// A gizmo to display the bounding boxes for navigation meshes inside the editor as a gizmo. 
    /// this gizmo uses scale as the extent of the bounding box and is not affected by rotation
    /// </summary>
    [GizmoComponent(typeof(SplineComponent), false)]
    public class SplineGizmo : BillboardingGizmo<SplineComponent>
    {
        private Entity debugEntity;
        private Entity debugEntityNodes;
        private Entity debugEntityNodeLink;
        private Entity debugEntitySegments;
        private Entity debugEntityOrbs;
        private Entity debugEntityOut;
        private Entity debugEntityIn;
        private float updateFrequency = 0.2f;
        private float updateTimer = 0.2f;

        //TODO per node

        public SplineGizmo(EntityComponent component) : base(component, "Spline gizmo", GizmoResources.SplineGizmo)
        {
        }

        protected override Entity Create()
        {
            debugEntity = new Entity();
            debugEntityNodes = new Entity();
            debugEntityNodeLink = new Entity();
            debugEntitySegments = new Entity();
            debugEntityOrbs = new Entity();
            debugEntityOut = new Entity();
            debugEntityIn = new Entity();
            debugEntity.AddChild(debugEntityNodes);
            debugEntity.AddChild(debugEntityNodeLink);
            debugEntity.AddChild(debugEntitySegments);
            debugEntity.AddChild(debugEntityOrbs);
            debugEntity.AddChild(debugEntityOut);
            debugEntity.AddChild(debugEntityIn);
            return debugEntity;
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
                foreach (var node in Component.Nodes)
                {
                    if (node == null || !node.Dirty)
                    {
                        break;
                    }

                    debugEntityNodes.RemoveAll<ModelComponent>();

                    if (Component.DebugInfo.Nodes)
                    {
                        DebugNodes(node);
                    }


                    if (node.Next == null)
                    {
                        break;
                    }

                    ClearChildren(debugEntitySegments);
                    ClearChildren(debugEntityOrbs);

                    debugEntitySegments.RemoveAll<ModelComponent>();
                    debugEntityNodeLink.RemoveAll<ModelComponent>();
                    debugEntityOut.RemoveAll<ModelComponent>();
                    debugEntityIn.RemoveAll<ModelComponent>();
                    node.MakeClean();



                    if (Component.DebugInfo.NodesLink)
                    {
                        DebugNodeLinks(node);
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
                            CreateSplinePoints(splinePoints);
                        }

                        if (Component.DebugInfo.Segments)
                        {
                            CreateSplineSegments(splinePoints);
                        }
                    }

                    if (Component.DebugInfo.OutHandler)
                    {
                        CreateOutHandler(node);
                    }

                    if (Component.DebugInfo.InHandler)
                    {
                        CreateInHandler(node);
                    }
                }
            }
        }

        private void CreateSplineSegments(Vector3[] splinePoints)
        {
            //var localPoints = new Vector3[splinePoints.Length];
            //for (int i = 0; i < splinePoints.Length - 1; i++)
            //{
            //    localPoints[i] = debugEntity.Transform.WorldToLocal(splinePoints[i]);
            //}

            //var bezierSegmentsMesh = new LineMesh(GraphicsDevice);
            //bezierSegmentsMesh.Build(localPoints);
            //debugEntitySegments.RemoveAll<ModelComponent>();
            //debugEntitySegments.Add(
            //    new ModelComponent
            //    {
            //        Model = new Model
            //        {
            //                GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Orange),
            //                new Mesh { Draw = bezierSegmentsMesh.MeshDraw }
            //        },
            //        RenderGroup = RenderGroup
            //    }
            //);

            ClearChildren(debugEntitySegments);

            var localPoints = new Vector3[splinePoints.Length];
            for (int i = 0; i < splinePoints.Length; i++)
            {
                localPoints[i] = debugEntity.Transform.WorldToLocal(splinePoints[i]);
            }

            for (int i = 0; i < localPoints.Length - 1; i++)
            {
                var lineMesh = new LineMesh(GraphicsDevice);
                lineMesh.Build(new Vector3[2] { localPoints[i], localPoints[i + 1] });

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

                debugEntitySegments.AddChild(segment);
                segment.Transform.Position = debugEntity.Transform.WorldToLocal(splinePoints[i]);
            }
        }

        private void CreateSplinePoints(Vector3[] splinePoints)
        {
            ClearChildren(debugEntityOrbs);

            for (int i = 0; i < splinePoints.Length; i++)
            {
                var pointMesh = new BulbMesh(GraphicsDevice, 0.2f);
                pointMesh.Build();

                var orb = new Entity()
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

                debugEntityOrbs.AddChild(orb);
                orb.Transform.Position = debugEntity.Transform.WorldToLocal(splinePoints[i]);
            }
        }


        private void DebugNodes(SplineNodeComponent splineNodeComponent)
        {
            var nodeMesh = new BulbMesh(GraphicsDevice, 0.4f);
            nodeMesh.Build();

            debugEntityNodes.Transform.Position = debugEntity.Transform.WorldToLocal(splineNodeComponent.Entity.Transform.WorldMatrix.TranslationVector);
            debugEntityNodes.Add(
                new ModelComponent
                {
                    Model = new Model
                    {
                                GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightSkyBlue),
                                new Mesh { Draw = nodeMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            );
        }


        private void DebugNodeLinks(SplineNodeComponent splineNodeComponent)
        {
            var nodeLinkLineMesh = new LineMesh(GraphicsDevice);
            nodeLinkLineMesh.Build(new Vector3[2] { splineNodeComponent.Entity.Transform.Position, splineNodeComponent.Next.Entity.Transform.Position - splineNodeComponent.Entity.Transform.Position });
            debugEntityNodeLink.RemoveAll<ModelComponent>();
            debugEntityNodeLink.Add(
                new ModelComponent
                {
                    Model = new Model
                    {
                            GizmoUniformColorMaterial.Create(GraphicsDevice, Color.Green),
                            new Mesh { Draw = nodeLinkLineMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            );
        }

        private void CreateOutHandler(SplineNodeComponent splineNodeComponent)
        {
            var outMesh = new BulbMesh(GraphicsDevice, 0.3f);
            outMesh.Build();

            debugEntityOut.Transform.Position = splineNodeComponent.TangentOut;
            debugEntityOut.Add(
                new ModelComponent
                {
                    Model = new Model
                    {
                                GizmoUniformColorMaterial.Create(GraphicsDevice, Color.LightGray),
                                new Mesh { Draw = outMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            );
        }

        private void CreateInHandler(SplineNodeComponent splineNodeComponent)
        {
            var inMesh = new BulbMesh(GraphicsDevice, 0.3f);
            inMesh.Build();

            debugEntityIn.Transform.Position = splineNodeComponent.TangentIn;
            debugEntityIn.Add(
                new ModelComponent
                {
                    Model = new Model
                    {
                                GizmoUniformColorMaterial.Create(GraphicsDevice, Color.DeepPink),
                                new Mesh { Draw = inMesh.MeshDraw }
                    },
                    RenderGroup = RenderGroup
                }
            );
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

        class BulbMesh
        {
            private const int Tesselation = 360 / 6;

            public MeshDraw MeshDraw;

            private Buffer vertexBuffer;
            private float _range;

            private readonly GraphicsDevice graphicsDevice;

            public BulbMesh(GraphicsDevice graphicsDevice, float range = 0.4f)
            {
                this.graphicsDevice = graphicsDevice;
                this._range = range;
            }

            public void Build()
            {
                var indices = new int[2 * Tesselation * 3];
                var vertices = new VertexPositionNormalTexture[(Tesselation + 1) * 3];

                int indexCount = 0;
                int vertexCount = 0;
                // the two rings
                for (int j = 0; j < 3; j++)
                {
                    var rotation = Matrix.Identity;
                    if (j == 1)
                    {
                        rotation = Matrix.RotationX((float)Math.PI / 2);
                    }
                    else if (j == 2)
                    {
                        rotation = Matrix.RotationY((float)Math.PI / 2);
                    }

                    for (int i = 0; i <= Tesselation; i++)
                    {
                        var longitude = (float)(i * 2.0 * Math.PI / Tesselation);
                        var dx = (float)Math.Cos(longitude);
                        var dy = (float)Math.Sin(longitude);
                        var normal = new Vector3(dx * _range, dy * _range, 0);
                        Vector3.TransformNormal(ref normal, ref rotation, out normal);

                        if (i < Tesselation)
                        {
                            indices[indexCount++] = vertexCount;
                            indices[indexCount++] = vertexCount + 1;
                        }

                        vertices[vertexCount++] = new VertexPositionNormalTexture(normal, normal, new Vector2(0));
                    }
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
