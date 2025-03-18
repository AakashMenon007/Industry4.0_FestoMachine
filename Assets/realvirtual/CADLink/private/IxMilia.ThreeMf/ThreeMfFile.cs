// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using ThreeMf.Collections;
using Ionic.Zip;

namespace ThreeMf
{
    public class ThreeMfFile
    {
        private const string RelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
        private const string ModelRelationshipType = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel";
        private const string RelsEntryPath = "/_rels/.rels";
        private const string DefaultModelEntryName = "3D/3dmodel";
        private const string ModelPathExtension = ".model";
        private const string TargetAttributeName = "Target";
        private const string IdAttributeName = "Id";
        private const string TypeAttributeName = "Type";

        public float Scale;

        internal static XName RelationshipsName = XName.Get("Relationships", RelationshipNamespace);
        private static XName RelationshipName = XName.Get("Relationship", RelationshipNamespace);

        public IList<ThreeMfModel> Models { get; } = new ListNonNull<ThreeMfModel>();


        internal static XElement GetRelationshipElement(string target, string id, string type)
        {
            return new XElement(RelationshipName,
                new XAttribute(TargetAttributeName, target),
                new XAttribute(IdAttributeName, id),
                new XAttribute(TypeAttributeName, type));
        }


        public static ThreeMfFile Load(string package, float scale)
        {
          
            var file = new ThreeMfFile();
            file.Scale = scale;
            var extractPath = Path.GetFullPath(package);
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            using (ZipFile archive = ZipFile.Read(package))
            {
                ZipEntry entry = archive[DefaultModelEntryName + ModelPathExtension];
                Stream contentStream = new MemoryStream();
                entry.Extract(contentStream);
                contentStream.Seek(0, SeekOrigin.Begin);
                var document = XDocument.Load(contentStream);
                var model = ThreeMfModel.LoadXml(document.Root, package,scale);
                file.Models.Add(model);
      
            }

            return file;
        }
    }
}