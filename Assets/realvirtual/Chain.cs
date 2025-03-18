using UnityEngine;
using Pixelplacement;
using UnityEditor;


//! game4automation namespace
namespace realvirtual
{
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/chain")]
    [RequireComponent(typeof(IChain))]
    //! Chain for moving objects and MUs on spline curves
    public class Chain : realvirtualBehavior
    {
        public GameObject ChainElement; //!< Chainelements which needs to be created along the chain
        public string NameChainElement;
        public Drive ConnectedDrive; //!< The drive which is moving ths chain
        public int NumberOfElements; //!< The number of elements which needs to be created along the chain
        public float StartPosition; //!< The start position on the chain (offset) for the first element
        public bool CreateElementeInEditMode = false;
        public bool CalculatedDeltaPosition = true; //!< true if the distance (DeltaPositoin) between the chain elements should be calculated based on number and chain length

        public float DeltaPosition; //! the calculated or set distance between two chain elements
        [ReadOnly] public float Length; //!< the calculated lenth of the spline

        private Spline spline;
        private GameObject newbeltelement;
        private IChain ichain;
        private bool usepath = false;
        private void Init()
        {
            if (CreateElementeInEditMode)
            {
                var anchors = GetComponentsInChildren<SplineAnchor>();
                foreach (var currAnchor in anchors)
                {
                    currAnchor.Initialize();
                }
            }
            ichain = gameObject.GetComponent<IChain>();
            usepath = ichain.UseSimulationPath();
        }
        private void Reset()
        {
            Init();
        }
        protected override void AfterAwake()
        {
            Init();
           
            var position = StartPosition;
            if (ichain != null )
            {
                Length = ichain.CalculateLength();
                if(!usepath) 
                    Length = Length * realvirtualController.Scale;
            }

            if (CalculatedDeltaPosition)
                DeltaPosition = Length / NumberOfElements;
            for (int i = 0; i < NumberOfElements; i++)
            {
                var j = i + 1;
                GameObject newelement;
                if (CreateElementeInEditMode)
                {
                    newelement = GetChildByName(NameChainElement + "_" + j);
                }
                else
                {
                    newelement = Instantiate(ChainElement, ChainElement.transform.parent);
                }
                newelement.transform.parent = this.transform;
                newelement.name = NameChainElement + "_" + j;
                var chainelement = newelement.GetComponent<IChainElement>();
                chainelement.UsePath = usepath;
                chainelement.StartPosition = position;
                chainelement.Position = position;
                chainelement.ConnectedDrive = ConnectedDrive;
                chainelement.Chain = this;
                position = position + DeltaPosition;
            }
            
        }
       public void Modify()
        {
            Init();
            var position = StartPosition;
            if (ichain != null)
            {
                Length = ichain.CalculateLength();
            }
            if (CalculatedDeltaPosition)
                DeltaPosition = Length / NumberOfElements;
            for (int i = 0; i < NumberOfElements; i++)
            {
                var newelement = Instantiate(ChainElement, ChainElement.transform.parent);
                newelement.transform.parent = this.transform;
                var j = i + 1;
                newelement.name =NameChainElement+"_" + j;
                var chainelement = newelement.GetComponent<IChainElement>();
                chainelement.UsePath = usepath;
                chainelement.StartPosition = position;
                chainelement.Position = position;
                chainelement.ConnectedDrive = ConnectedDrive;
                chainelement.Chain = this;
                chainelement.InitPos(position);
                position = position + DeltaPosition;
            }
        }
       public Vector3 GetPosition(float normalizedposition)
        {
            if (ichain != null)
                return ichain.GetPosition(normalizedposition,true);
            else
            {
                return Vector3.zero;
            }
        }
        public Vector3 GetTangent(float normalizedposition)
        {
            return ichain.GetDirection(normalizedposition,true);
        }
        
    }
}