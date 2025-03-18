Shader "DNWA/SimpleShader" {
            SubShader { Pass {
                BindChannels { Bind "Color",color }
				ZWrite Off ZTest Always Cull Off Fog { Mode Off }
            } } }