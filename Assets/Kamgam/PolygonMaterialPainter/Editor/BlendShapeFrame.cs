using UnityEngine;
using System.Collections.Generic;

namespace Kamgam.PolygonMaterialPainter
{
    public class BlendShapeFrame
    {
        public string Name;
        public float Weight;
        public Vector3[] DeltaVertices;
        public Vector3[] DeltaNormals;
        public Vector3[] DeltaTangents;

        public BlendShapeFrame(string name, float weight, int vertexCount)
        {
            Name = name;
            Weight = weight;
            DeltaVertices = new Vector3[vertexCount];
            DeltaNormals = new Vector3[vertexCount];
            DeltaTangents = new Vector3[vertexCount];
        }

        public static List<List<BlendShapeFrame>> CreateFromMesh(Mesh mesh)
        {
            var result = new List<List<BlendShapeFrame>>();

            if (mesh.blendShapeCount == 0)
                return result;

            for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; shapeIndex++)
            {
                var frames = CreateFromMesh(mesh, shapeIndex);
                if (frames.Count > 0)
                {
                    result.Add(frames);
                }
            }

            return result;
        }

        public static List<BlendShapeFrame> CreateFromMesh(Mesh mesh, int shapeIndex)
        {
            var result = new List<BlendShapeFrame>();

            if (mesh.blendShapeCount <= shapeIndex)
                return result;

            int frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);
            if (frameCount == 0)
                return result;

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var frame = CreateFromMesh(mesh, shapeIndex, frameIndex);
                if (frame != null)
                    result.Add(frame);
            }

            return result;
        }

        public static BlendShapeFrame CreateFromMesh(Mesh mesh, int shapeIndex, int frameIndex)
        {
            if (mesh.blendShapeCount <= shapeIndex)
                return null;

            int frameCount = mesh.GetBlendShapeFrameCount(shapeIndex);

            if (frameCount <= frameIndex)
                return null;

            string name = mesh.GetBlendShapeName(shapeIndex);
            var weight = mesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);

            var frame = new BlendShapeFrame(name, weight, mesh.vertexCount);
            mesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, frame.DeltaVertices, frame.DeltaNormals, frame.DeltaTangents);

            return frame;
        }

        public static void ApplyToMesh(Mesh mesh, List<List<BlendShapeFrame>> blendShapes)
        {
            mesh.ClearBlendShapes();
            for (int s = 0; s < blendShapes.Count; s++)
            {
                int frameCount = blendShapes[s].Count;
                for (int f = 0; f < frameCount; f++)
                {
                    string name = blendShapes[s][f].Name;
                    var weight = blendShapes[s][f].Weight;
                    mesh.AddBlendShapeFrame(name, weight, blendShapes[s][f].DeltaVertices, blendShapes[s][f].DeltaNormals, blendShapes[s][f].DeltaTangents);
                }
            }
        }

        /// <summary>
        /// Removes all vertices that are not in the list of remaining vertex INDICES.
        /// </summary>
        /// <param name="remainingVertexIndices"></param>
        public void TrimVertices(List<int> remainingVertexIndices)
        {
            Vector3[] newDeltaVertices = new Vector3[remainingVertexIndices.Count];
            copyVerticesByIndex(DeltaVertices, newDeltaVertices, remainingVertexIndices);
            DeltaVertices = newDeltaVertices;

            Vector3[] newDeltaNormals = new Vector3[remainingVertexIndices.Count];
            copyVerticesByIndex(DeltaNormals, newDeltaNormals, remainingVertexIndices);
            DeltaNormals = newDeltaNormals;

            Vector3[] newDeltaTangents = new Vector3[remainingVertexIndices.Count];
            copyVerticesByIndex(DeltaTangents, newDeltaTangents, remainingVertexIndices);
            DeltaTangents = newDeltaTangents;
        }

        protected void copyVerticesByIndex(Vector3[] source, Vector3[] target, List<int> remainingVertexIndices)
        {
            Debug.Assert(remainingVertexIndices.Count == target.Length);

            int targetIndex = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (remainingVertexIndices.Contains(i))
                {
                    target[targetIndex] = source[i];
                    targetIndex++;
                }
            }
        }
    }
}

