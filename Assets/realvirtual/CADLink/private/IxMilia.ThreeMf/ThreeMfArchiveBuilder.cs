﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ThreeMf
{
    internal class ThreeMfArchiveBuilder : IDisposable
    {
        private const string ContentTypesNamespace = "http://schemas.openxmlformats.org/package/2006/content-types";
        private const string RelsExtension = "rels";
        private const string ModelExtension = "model";
        private const string ExtensionAttributeName = "Extension";
        private const string ContentTypeAttributeName = "ContentType";
        private const string PartNameAttributeName = "PartName";
        private const string DefaultRelationshipPrefix = "rel";
        private const string ContentTypesPath = "/[Content_Types].xml";
        private const string ModelRelationshipPath = "/3D/_rels/3dmodel.model.rels";

        private const string ModelContentType = "application/vnd.ms-package.3dmanufacturing-3dmodel+xml";
        private const string RelsContentType = "application/vnd.openxmlformats-package.relationships+xml";

        private static XName TypesName = XName.Get("Types", ContentTypesNamespace);
        private static XName DefaultName = XName.Get("Default", ContentTypesNamespace);
        internal static XName OverrideName = XName.Get("Override", ContentTypesNamespace);

     
        private XElement _modelRelationships;
        private XElement _contentTypes;
        private HashSet<string> _seenContentTypes;
        private int _currentRelationshipId = 0;

        private static XmlWriterSettings WriterSettings = new XmlWriterSettings()
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  "
        };

     
        private static string GetNormalizedArchivePath(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            return fullPath.Length > 0 && fullPath[0] == '/'
                ? fullPath.Substring(1)
                : fullPath;
        }

    
        public virtual void WriteBinaryDataToArchive(string fullPath, byte[] data, string relationshipType, string contentType, bool overrideContentType = false)
        {
            // game4automation deleted
            
        }

        private static XElement GetDefaultContentType(string extension, string contentType)
        {
            return new XElement(DefaultName,
                new XAttribute(ExtensionAttributeName, extension),
                new XAttribute(ContentTypeAttributeName, contentType));
        }

        public string NextRelationshipId()
        {
            var rel = DefaultRelationshipPrefix + _currentRelationshipId;
            _currentRelationshipId++;
            return rel;
        }

        #region IDisposable

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                  
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}
