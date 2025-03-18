﻿// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using UnityEngine;
using realvirtual;

namespace realvirtual
{
    //! Lamp color enum
    public enum LAMPCOLOR
    {
        White,
        Yellow,
        Green,
        Red,
    }

    [SelectionBase]
    //! Lamp for displaying a lamp in the scene
    public class Lamp : realvirtualBehavior
    {

        [Header("Settings")]
        public Material MaterialOff; //!<  Material for off state
        public Material MaterialOn; //!<  Material for on state
    
        public bool UseHalo; //!<  Use halo for lamp
        public float Diameter; //!<  Diameter of lamp in mm
        public float Height; //!< Height of lamp in mm
        public float LightRange; //!< Light range of lamp in mm
    
        [Header("Lamp IO's")]
        public bool Flashing = false; //!<  True if lamp should be flashing.
        public float Period = 1; //!<  Lamp fleshing period in seconds.
        public bool LampOn = false; //!  Lamp is on if true.

        private Material _coloron;
        private Material _coloroff;

        private MeshRenderer _meshrenderer;

        private float _timeon;
        private int _incolorbefore;
        private bool _flashingbefore;
        private float _periodbefore;
        private bool _lamponbefore;
        private bool _lampon;
        private Light _lamp;
        private Behaviour _helo;
        private Color _color;
        private Material _material;
        private Transform _cylinder;


        
        // Use this for initialization
        private void InitLight()
        {
            _meshrenderer = GetMeshRenderer();
            _material = MaterialOff;
            
            if (_lamp != null)
            {
                if (realvirtualController!=null)
                   _lamp.range = LightRange / realvirtualController.Scale;
            }
            if (_cylinder != null)
            {
                if (realvirtualController!=null)
                      _cylinder.localScale = new Vector3(Diameter/realvirtualController.Scale,Height/(2*realvirtualController.Scale),Diameter/realvirtualController.Scale);   
            }
        
        }

        private void OnValidate()
        {
            InitLight();
        }

        public void Start()
        {
      
            _timeon = Time.time;
            _lamponbefore = LampOn;
            _lamp = GetComponentInChildren<Light>();
            _helo = (Behaviour)GetComponent("Halo");

            InitLight();
            Off();
        
        }

        //! Turns the lamp on.
        public void On()
        {
            LampOn = true;
            _meshrenderer.material = MaterialOn;
            if (_lamp)
            {
                _lamp.enabled = true;
            }
            if (_helo && UseHalo)
            {
                _helo.enabled = true;
            }
       
        }

        //!  Turns the lamp off.
        public void Off()
        {
            LampOn = false;
            _meshrenderer.material = MaterialOff;
       
            if (_lamp)
            {
                _lamp.enabled = false;
            }
            if (_helo && UseHalo)
            {
                _helo.enabled = false;
            }
        }


        // Update is called once per frame
        void Update()
        {
            
            if (Flashing)
            {
                float delta = Time.time - _timeon;
                if (!_lampon && delta > Period)
                {
                    _lampon = true;
                }
                else
                {
                    if (_lampon && delta > Period / 2)
                    {
                        _lampon = false;
                    }
                }
            }

            if (!Flashing)
            {
                _lampon = LampOn;
            }

            if (_lampon && _lampon != _lamponbefore)
            {
                On();
                _timeon = Time.time;
            }

            if (!_lampon && _lampon != _lamponbefore)
            {
                Off();
            }
            
            _lamponbefore = _lampon;
        }
    }
}