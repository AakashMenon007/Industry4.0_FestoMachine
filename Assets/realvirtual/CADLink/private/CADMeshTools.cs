// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System.Collections.Generic;
using UnityEngine;

namespace realvirtual
{
    public static class CADMeshTools
    {
        
      
        
        public static void DistinctVertices(ref Mesh myMesh)
        {
            // Get mesh info from attached mesh
            var myVertices = myMesh.vertices;
            var myTriangles = myMesh.triangles;
            var myUV = myMesh.uv;
            var myNormals = myMesh.normals;
            // Set up new arrays to use with rebuilt mesh
            Vector3[] newVertices = new Vector3[myTriangles.Length];
            var newUV = new Vector2[myTriangles.Length];
            var newNormals = new Vector3[myTriangles.Length];
            var newTriangles = new int[myTriangles.Length];
            int i = 0;
            // Rebuild mesh so that every triangle has unique vertices
            for (i = 0; i < myTriangles.Length; i++)
            {
                newVertices[i] = myVertices[myTriangles[i]];
                if (myUV.Length > 0)
                    newUV[i] = myUV[myTriangles[i]];
                if (myNormals.Length > 0)
                   newNormals[i] = myNormals[myTriangles[i]];
                newTriangles[i] = i;
            }
            // Assign new mesh
            myMesh.vertices = newVertices;
            if (myUV.Length > 0)
                myMesh.uv = newUV;
            if (myNormals.Length > 0)
                myMesh.normals = newNormals;
            myMesh.triangles = newTriangles;
        }

        public static void ScaleMesh(ref Mesh myMesh, float scale)
        {
            Vector3[] _baseVertices = myMesh.vertices;;
            
          
            var vertices = new Vector3[myMesh.vertexCount];
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = _baseVertices[i];
                vertex.x = vertex.x * scale;
                vertex.y = vertex.y * scale;
                vertex.z = vertex.z * scale;
                vertices[i] = vertex;
            }
            myMesh.vertices = vertices;
        }
        
        public static void CalculateUVS(ref Mesh myMesh, float UVScaleFactor)
        {
            var uvs = new Vector2[myMesh.vertexCount];
            for (int index = 0; index < myMesh.vertexCount; index += 3)
            {
                // Get the three vertices bounding this triangle.
                Vector3 v1 = myMesh.vertices[myMesh.triangles[index]];
                Vector3 v2 = myMesh.vertices[myMesh.triangles[index + 1]];
                Vector3 v3 = myMesh.vertices[myMesh.triangles[index + 2]];

                // Compute a vector perpendicular to the face.
                Vector3 normal = Vector3.Cross(v3 - v1, v2 - v1);

                // Form a rotation that points the z+ axis in this perpendicular direction.
                // Multiplying by the inverse will flatten the triangle into an xy plane.
                Quaternion rotation = Quaternion.Inverse(Quaternion.LookRotation(normal));

                // Assign the uvs, applying a scale factor to control the texture tiling.
                uvs[myMesh.triangles[index]] = (Vector2) (rotation * v1) * UVScaleFactor;
                uvs[myMesh.triangles[index + 1]] = (Vector2) (rotation * v2) * UVScaleFactor;
                uvs[myMesh.triangles[index + 2]] = (Vector2) (rotation * v3) * UVScaleFactor;

                myMesh.uv = uvs;
            }
        }


        /// <summary>
        ///     Recalculate the normals of a mesh based on an angle threshold. This takes
        ///     into account distinct vertices that have the same position.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="angle">
        ///     The smoothing angle. Note that triangles that already share
        ///     the same vertex will be smooth regardless of the angle! 
        /// </param>
        public static void RecalculateNormals(this Mesh mesh, float angle)
        {
            var cosineThreshold = Mathf.Cos(angle * Mathf.Deg2Rad);

            var vertices = mesh.vertices;
            var normals = new Vector3[vertices.Length];

            // Holds the normal of each triangle in each sub mesh.
            var triNormals = new Vector3[mesh.subMeshCount][];

            var dictionary = new Dictionary<VertexKey, List<VertexEntry>>(vertices.Length);

            for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
            {
                var triangles = mesh.GetTriangles(subMeshIndex);

                triNormals[subMeshIndex] = new Vector3[triangles.Length / 3];

                for (var i = 0; i < triangles.Length; i += 3)
                {
                    int i1 = triangles[i];
                    int i2 = triangles[i + 1];
                    int i3 = triangles[i + 2];

                    // Calculate the normal of the triangle
                    Vector3 p1 = vertices[i2] - vertices[i1];
                    Vector3 p2 = vertices[i3] - vertices[i1];
                    Vector3 normal = Vector3.Cross(p1, p2).normalized;
                    int triIndex = i / 3;
                    triNormals[subMeshIndex][triIndex] = normal;

                    List<VertexEntry> entry;
                    VertexKey key;

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i1]), out entry))
                    {
                        entry = new List<VertexEntry>(4);
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i1));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i2]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i2));

                    if (!dictionary.TryGetValue(key = new VertexKey(vertices[i3]), out entry))
                    {
                        entry = new List<VertexEntry>();
                        dictionary.Add(key, entry);
                    }

                    entry.Add(new VertexEntry(subMeshIndex, triIndex, i3));
                }
            }

            // Each entry in the dictionary represents a unique vertex position.

            foreach (var vertList in dictionary.Values)
            {
                for (var i = 0; i < vertList.Count; ++i)
                {
                    var sum = new Vector3();
                    var lhsEntry = vertList[i];

                    for (var j = 0; j < vertList.Count; ++j)
                    {
                        var rhsEntry = vertList[j];

                        if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                        {
                            sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                        }
                        else
                        {
                            // The dot product is the cosine of the angle between the two triangles.
                            // A larger cosine means a smaller angle.
                            var dot = Vector3.Dot(
                                triNormals[lhsEntry.MeshIndex][lhsEntry.TriangleIndex],
                                triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex]);
                            if (dot >= cosineThreshold)
                            {
                                sum += triNormals[rhsEntry.MeshIndex][rhsEntry.TriangleIndex];
                            }
                        }
                    }

                    normals[lhsEntry.VertexIndex] = sum.normalized;
                }
            }

            mesh.normals = normals;
        }

        private struct VertexKey
        {
            private readonly long _x;
            private readonly long _y;
            private readonly long _z;

            // Change this if you require a different precision.
            private const long Tolerance = 10000;

            // Magic FNV values. Do not change these.
            private const long FNV32Init = 0x811c9dc5;
            private const long FNV32Prime = 0x01000193;

            public VertexKey(Vector3 position)
            {
                _x = (long) (Mathf.Round(position.x * Tolerance));
                _y = (long) (Mathf.Round(position.y * Tolerance));
                _z = (long) (Mathf.Round(position.z * Tolerance));
            }

            public override bool Equals(object obj)
            {
                var key = (VertexKey) obj;
                return _x == key._x && _y == key._y && _z == key._z;
            }

            public override int GetHashCode()
            {
                long rv = FNV32Init;
                rv ^= _x;
                rv *= FNV32Prime;
                rv ^= _y;
                rv *= FNV32Prime;
                rv ^= _z;
                rv *= FNV32Prime;

                return rv.GetHashCode();
            }
        }

        private struct VertexEntry
        {
            public int MeshIndex;
            public int TriangleIndex;
            public int VertexIndex;

            public VertexEntry(int meshIndex, int triIndex, int vertIndex)
            {
                MeshIndex = meshIndex;
                TriangleIndex = triIndex;
                VertexIndex = vertIndex;
            }
        }
    }
}