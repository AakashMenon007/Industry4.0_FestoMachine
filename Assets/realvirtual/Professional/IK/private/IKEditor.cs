// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[HelpURL("https://doc.realvirtual.io/components-and-scripts/motion/robot-inverse-kinematics")]
 public static class IKEditor
 {
#pragma warning disable 0618
#pragma warning disable 0414
     
     private static PlayModeState _currentState = PlayModeState.Stopped;
     public enum PlayModeState
     {
         Stopped,
         Playing,
         Paused
     }
 
     public delegate void
         OnStartPlaymodeDelegate(); //!< Delegate function for GameObjects entering the Sensor.

     public static event OnStartPlaymodeDelegate OnStartPlaymode;
     
     static IKEditor()
     {
         EditorApplication.playmodeStateChanged = OnUnityPlayModeChanged;
     }
     
     private static void OnPlayModeChanged(PlayModeState currentMode, PlayModeState changedMode)
     {
         if (currentMode == PlayModeState.Stopped && changedMode == PlayModeState.Playing)
         {
             if (OnStartPlaymode != null)
                 OnStartPlaymode.Invoke();
         }
         
         if (currentMode == PlayModeState.Playing && changedMode == PlayModeState.Stopped)
         {
             if (OnStartPlaymode != null)
                 OnStartPlaymode.Invoke();
         }
     }
     
     private static void OnUnityPlayModeChanged()
     {
         var changedState = PlayModeState.Stopped;
         switch (_currentState)
         {
             case PlayModeState.Stopped:
                 if (EditorApplication.isPlayingOrWillChangePlaymode)
                 {
                     changedState = PlayModeState.Playing;
                 }
                 break;
             case PlayModeState.Playing:
                 if (EditorApplication.isPaused)
                 {
                     changedState = PlayModeState.Paused;
                 }
                 else
                 {
                     changedState = PlayModeState.Stopped;
                 }
                 break;
             case PlayModeState.Paused:
                 if (EditorApplication.isPlayingOrWillChangePlaymode)
                 {
                     changedState = PlayModeState.Playing;
                 }
                 else
                 {
                     changedState = PlayModeState.Stopped;
                 }
                 break;
             default:
                 throw new ArgumentOutOfRangeException();
         }
 
         // Fire PlayModeChanged event.
         OnPlayModeChanged(_currentState, changedState);
 
         // Set current state.
         _currentState = changedState;
     }
 
 }
#endif
