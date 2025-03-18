// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using UnityEngine;
using ThreeMf.Extensions;

namespace ThreeMf
{
    public class ThreeMfMesh
    {
        private static XName VerticesName = XName.Get("vertices", ThreeMfModel.ModelNamespace);
        private static XName TrianglesName = XName.Get("triangles", ThreeMfModel.ModelNamespace);

        public IList<ThreeMfTriangle> Triangles { get; } = new List<ThreeMfTriangle>();

        public List<Vector3> uVertices = new List<Vector3>();
        public List<int> uTriangles = new List<int>();
        
        private const string V1AttributeName = "v1";
        private const string V2AttributeName = "v2";
        private const string V3AttributeName = "v3";

        internal XElement ToXElement(Dictionary<ThreeMfResource, int> resourceMap)
        {
            var vertices = Triangles.SelectMany(t => new[] { t.V1, t.V2, t.V3 }).Distinct().ToList();
            var verticesXml = vertices.Select(v => v.ToXElement());
            var trianglesXml = Triangles.Select(t => t.ToXElement(vertices, resourceMap));
            return new XElement(ThreeMfObject.MeshName,
                new XElement(VerticesName, verticesXml),
                new XElement(TrianglesName, trianglesXml));
        }

        internal static ThreeMfMesh ParseMesh(XElement element, Dictionary<int, ThreeMfResource> resourceMap, float scale)
        {
            if (element == null)
            {
                
                return null;
            }

            var vertices = new List<ThreeMfVertex>();
            var mesh = new ThreeMfMesh();
 
            foreach (var vertexElement in element.Element(VerticesName).Elements(ThreeMfVertex.VertexName))
            {
                var vertex = ThreeMfVertex.ParseVertex(vertexElement);
                vertices.Add(vertex);
                
                
                mesh.uVertices.Add(new Vector3((float)-vertex.X*scale,(float)vertex.Y*scale,(float)vertex.Z*scale)); // y and z changed for unity
            }
          
            

     
            foreach (var triangleElement in element.Element(TrianglesName).Elements(ThreeMfTriangle.TriangleName))
            {
                var triangle = ThreeMfTriangle.ParseTriangle(triangleElement, vertices, resourceMap);
                mesh.Triangles.Add(triangle);
                
                var v1Index = triangleElement.AttributeIntValueOrThrow(V1AttributeName);
                var v2Index = triangleElement.AttributeIntValueOrThrow(V2AttributeName);
                var v3Index = triangleElement.AttributeIntValueOrThrow(V3AttributeName);
                
                mesh.uTriangles.Add(v1Index);
                mesh.uTriangles.Add(v2Index);
                mesh.uTriangles.Add(v3Index);
            }


            mesh.uTriangles.Reverse();
            return mesh;
        }
    }
}
