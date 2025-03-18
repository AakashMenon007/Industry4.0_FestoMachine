﻿using System;
using NaughtyAttributes;
#if REALVIRTUAL_PIXYZ && UNITY_EDITOR_WIN
using Pixyz;
#endif
using UnityEngine;

namespace realvirtual
{
    public enum CADStatus
    {
        Updated,
        Moved,
        ToBeChanged,
        Changed,
        Deleted,
        Added
    };



    //! CAD and Metadata information on Gameobjects, used for Updating CAD Data
    public class CAD : realvirtualBehavior
    {
        public bool Keep; //!< Keep this without touching by Updates
        [OnValueChanged("KeepChildren")]
        public bool KeepIncludingChildren; //!< Keep this including its children without touching it by updates
        [ReadOnly] public CADStatus Status; //!< Current Cad Status (Updated, Moved, ToBeChanged, Changed, Deleted, Added)
        public CAD KeepAndReplaceByUpdate; //!< Keep this and replaces this by referenced Update (IDs are made identical)
    
        public bool DisplayFullInfo = false; //!< true to display full CAD Part info
       
   
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public int Version;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public int Instance;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public string ID;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public string ImportedDate;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public bool IsAssembly;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public CAD UpperAssembly;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public CAD IsMeshFor;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public CAD RelatedCurrent;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public CAD RelatedUpdate;
        [ShowIf("DisplayFullInfo")]
        [ReadOnly] public bool IsTopOfAdded;
        [HideInInspector][ReadOnly] public CAD NewCurrentTop;
        
        private string jt_prop_name;
        private string partiontype;
        private string breptype;

        private void KeepChildren()
        {
            var objs = Global.GatherObjects(this.gameObject);
            Keep = KeepIncludingChildren;
    
            foreach (GameObject obj in objs)
            {
                var cadobj = obj.GetComponent<CAD>();
                if (cadobj != null)
                {
                    cadobj.Keep = KeepIncludingChildren;
                }
            }
        }
        
        // Sets the JT Metadata based on PIXYZ import
        public string SetJTMetadata()
        {
            jt_prop_name = "";
            partiontype = "";
            breptype = "";
            #if REALVIRTUAL_PIXYZ &&  UNITY_EDITOR_WIN
            var metadatas = GetComponents<Pixyz.Import.Metadata>();
            foreach (var metadata in metadatas)
            {
                if (metadata.containsProperty("JT_PROP_NAME"))
                {
                    jt_prop_name = metadata.getProperty("JT_PROP_NAME");
                }
                
                if (metadata.containsProperty("PartitionType"))
                {
                    partiontype = metadata.getProperty("PartitionType");
                }


                if (metadata.containsProperty("JT_PROP_ORIGINATING_BREPTYPE"))
                {
                    breptype =  metadata.getProperty("JT_PROP_ORIGINATING_BREPTYPE");
                }
            }
            #endif
            // Get JT Properties
            if (jt_prop_name != "")
            {
                string[] separator = {";", ":"};
                string[] strslit = jt_prop_name.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                Name = strslit[0];
                Version = int.Parse(strslit[1]);
                Instance = int.Parse(strslit[2]);
                ID = Name + "/" + Instance;
                
                ImportedDate = DateTime.Today.ToString();
            }
            else
            {
                ID = name;
            }

            if (breptype == "XTBrep")
            {
                var upperpart = gameObject.transform.parent;
                var uppercad = upperpart.GetComponent<CAD>();
                ID = uppercad.ID + ":" + ID;
            }
            
            // Set JT Properties for Meshes
            if (jt_prop_name == "")
            {
                var mesh = gameObject.GetComponent<MeshFilter>();
                if (mesh != null)
                {
                    // Set JT Properties of upper part
                    var upperpart = gameObject.transform.parent;
                    var uppercad = upperpart.GetComponent<CAD>();
                    if (uppercad != null)
                    {

                        Name = uppercad.Name;
                        Version = uppercad.Version;
                        Instance = uppercad.Instance;
                        IsMeshFor = uppercad;
                        ID = uppercad.ID + "/" + Instance + "/Mesh";
                        ImportedDate = DateTime.Today.ToString();

                        // Get upper Assembly
                        var upperassembly = upperpart.transform.parent;
                        var upperassycad = upperassembly.GetComponent<CAD>();
                        if (upperassycad != null)
                        {
                            if (upperassycad.IsAssembly)
                            {
                                UpperAssembly = upperassycad;
                            
                            }
                        }
                    }
                }

            }
        
            if (jt_prop_name != "")
            {
                var upperpart = gameObject.transform.parent;
                if (upperpart != null)
                {
                    var uppercad = upperpart.GetComponent<CAD>();
                    if (uppercad != null)
                    {
                        if (uppercad.IsAssembly)
                        {
                            UpperAssembly = uppercad;
                        }
                    }
                }
            }
            
            if (partiontype == "Assembly")
            {
                IsAssembly = true;
            }
            
            if (!ReferenceEquals(KeepAndReplaceByUpdate,null))
            {
                ID = KeepAndReplaceByUpdate.ID;
            }
            return jt_prop_name;
        }

        new void Awake()
        {
            
        }
    }

}

