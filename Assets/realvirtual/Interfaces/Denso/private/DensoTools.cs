// realvirtual.io (formerly game4automation) (R) a Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/unternehmen/lizenz  

using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace realvirtual
{
    // Custom class for IP address
    [Serializable]
    public class IPAddress
    {
        public int Address1;
        public int Address2;
        public int Address3;
        public int Address4;

        public string GetAddress()
        {
            return Address1.ToString() + "." + Address2.ToString() + "." + Address3.ToString() + "." +
                   Address4.ToString();
        }
    }

    public struct RobotArea
    {
        public Matrix4x4 Transform;
        public Pose UnityPose;
        public Vector3 Size;
        public int Num;
        public int IOLine;
        public int PosVar;
        public int ErrorType;
        public bool Enabled;

        public RobotArea(int number)
        {
            this.Num = number;
            this.IOLine = -1;
            this.PosVar = -1;
            this.ErrorType = -1;
            this.Enabled = false;
            this.Transform = Matrix4x4.identity;
            this.UnityPose = new Pose();
            this.Size = Vector3.zero;
        }
    }

    // stucture for storing information about robot safety areas
    public struct RobotSafetyArea
    {
        public string Name;
        public Matrix4x4 Transform;
        public Pose UnityPose;
        public Vector3 Size;
        public int MonitoringIndex;

        public RobotSafetyArea(int number)
        {
            this.Name = "";
            this.MonitoringIndex = number;
            this.Transform = Matrix4x4.identity;
            this.Size = Vector3.zero;
            this.UnityPose = new Pose();
        }
    }


    public static class DensoTools
    {
        public enum ReferenceFrame
        {
            WorldFrame,
            LocalFrame
        };

        public enum RotationAxis
        {
            AxisX = 0,
            AxisY = 1,
            AxisZ = 2
        }


        public static void DrawBox(DensoInterface denso, Vector3 boxSize, Pose areaTransform, Color boxColor)
        {
            GL.Begin(GL.LINES);
            GL.Color(boxColor);
            DrawLine(denso.transform.TransformPoint(areaTransform.position + areaTransform.rotation * (-boxSize)),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, boxSize.y, -boxSize.z))));
            DrawLine(denso.transform.TransformPoint(areaTransform.position + areaTransform.rotation * (-boxSize)),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, -boxSize.y, boxSize.z))));
            DrawLine(denso.transform.TransformPoint(areaTransform.position + areaTransform.rotation * (-boxSize)),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, -boxSize.y, -boxSize.z))));

            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, boxSize.y, boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, boxSize.y, boxSize.z))));
            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, boxSize.y, boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, -boxSize.y, boxSize.z))));
            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, boxSize.y, boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, boxSize.y, -boxSize.z))));

            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, -boxSize.y, boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, boxSize.y, boxSize.z))));
            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, -boxSize.y, boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, -boxSize.y, -boxSize.z))));
            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, -boxSize.y, boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, -boxSize.y, boxSize.z))));

            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, boxSize.y, -boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(-boxSize.x, boxSize.y, -boxSize.z))));
            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, boxSize.y, -boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, boxSize.y, boxSize.z))));
            DrawLine(
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, boxSize.y, -boxSize.z))),
                denso.transform.TransformPoint(areaTransform.position +
                                               areaTransform.rotation *
                                               (new Vector3(boxSize.x, -boxSize.y, -boxSize.z))));
            GL.End();
        }

        public static void DrawArrowX(float xCoord, float yCoord, float zCoord, float arrowRadius, float arrowDepth,
            Color arrowColor, ReferenceFrame refFrame, Pose preTransformation, Transform objectTransformation)
        {
            Vector3 p = new Vector3(xCoord, yCoord, zCoord);

            float y1 = 0;
            float z1 = 0;
            arrowDepth = arrowDepth / 2;

            GL.Begin(GL.TRIANGLES);
            GL.Color(arrowColor);
            for (int i = 0; i <= 360; i++)
            {
                float angle = (float) (((float) i) / 57.29577957795135);
                float z2 = arrowRadius * Mathf.Sin(angle);
                float y2 = arrowRadius * Mathf.Cos(angle);
                switch (refFrame)
                {
                    case ReferenceFrame.WorldFrame:
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(arrowDepth, 0, 0)));
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(-arrowDepth, y1, z1)));
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(-arrowDepth, y2, z2)));
                        break;
                    case ReferenceFrame.LocalFrame:
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(arrowDepth, 0, 0))));
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(-arrowDepth, y1, z1))));
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(-arrowDepth, y2, z2))));
                        break;
                }

                y1 = y2;
                z1 = z2;
            }

            GL.End();
        }

        public static void DrawArrowY(float xCoord, float yCoord, float zCoord, float arrowRadius, float arrowDepth,
            Color arrowColor, ReferenceFrame refFrame, Pose preTransformation, Transform objectTransformation)
        {
            Vector3 p = new Vector3(xCoord, yCoord, zCoord);

            float z1 = 0;
            float x1 = 0;
            arrowDepth = arrowDepth / 2;

            GL.Begin(GL.TRIANGLES);
            GL.Color(arrowColor);
            for (int i = 0; i <= 360; i++)
            {
                float angle = (float) (((float) i) / 57.29577957795135);
                float x2 = arrowRadius * Mathf.Sin(angle);
                float z2 = arrowRadius * Mathf.Cos(angle);
                switch (refFrame)
                {
                    case ReferenceFrame.WorldFrame:
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(0, 0, arrowDepth)));
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(x1, z1, -arrowDepth)));
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(x2, z2, -arrowDepth)));
                        break;
                    case ReferenceFrame.LocalFrame:
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(0, arrowDepth, 0))));
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(x1, -arrowDepth, z1))));
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(x2, -arrowDepth, z2))));
                        break;
                }

                z1 = z2;
                x1 = x2;
            }

            GL.End();
        }


        public static void DrawArrowZ(float xCoord, float yCoord, float zCoord, float arrowRadius, float arrowDepth,
            Color arrowColor, ReferenceFrame refFrame, Pose preTransformation, Transform objectTransformation)
        {
            Vector3 p = new Vector3(xCoord, yCoord, zCoord);

            float y1 = 0;
            float x1 = 0;
            arrowDepth = arrowDepth / 2;

            GL.Begin(GL.TRIANGLES);
            GL.Color(arrowColor);

            for (int i = 0; i <= 360; i++)
            {
                float angle = (float) (((float) i) / 57.29577957795135);
                float x2 = arrowRadius * Mathf.Sin(angle);
                float y2 = arrowRadius * Mathf.Cos(angle);
                switch (refFrame)
                {
                    case ReferenceFrame.WorldFrame:
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(0, arrowDepth, 0)));
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(x1, -arrowDepth, y1)));
                        GL.Vertex(p + preTransformation.rotation * (new Vector3(x2, -arrowDepth, y2)));
                        break;
                    case ReferenceFrame.LocalFrame:
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(0, 0, arrowDepth))));
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(x1, y1, -arrowDepth))));
                        GL.Vertex(objectTransformation.TransformPoint(objectTransformation.InverseTransformPoint(p) +
                                                                      preTransformation.rotation *
                                                                      (new Vector3(x2, y2, -arrowDepth))));
                        break;
                }

                y1 = y2;
                x1 = x2;
            }

            GL.End();
        }


        public static void DrawGrid(int rows, int columns, Color gridColor)
        {
            // Render grid centered over (0,0)
            GL.Begin(GL.LINES);
            GL.Color(gridColor);
            for (int i = -rows; i <= rows; i++)
            {
                GL.Vertex3(-rows, 0, i);
                GL.Vertex3(columns, 0, i);
            }

            for (int i = -columns; i <= columns; i++)
            {
                GL.Vertex3(i, 0, -columns);
                GL.Vertex3(i, 0, rows);
            }

            GL.End();
        }


        private static Matrix4x4 BuildRotationMatrix(double theta, RotationAxis axis)
        {
            // Abbreviations for the different angular functions
            float ct = (float) Math.Cos(theta);
            float st = (float) Math.Sin(theta);
            Matrix4x4 rotMat = Matrix4x4.identity;
            switch (axis)
            {
                case RotationAxis.AxisX:
                    rotMat[0, 0] = 1;
                    rotMat[0, 1] = 0;
                    rotMat[0, 2] = 0;

                    rotMat[1, 0] = 0;
                    rotMat[1, 1] = ct;
                    rotMat[1, 2] = -st;

                    rotMat[2, 0] = 0;
                    rotMat[2, 1] = st;
                    rotMat[2, 2] = ct;
                    break;
                case RotationAxis.AxisY:
                    rotMat[0, 0] = ct;
                    rotMat[0, 1] = 0;
                    rotMat[0, 2] = st;

                    rotMat[1, 0] = 0;
                    rotMat[1, 1] = 1;
                    rotMat[1, 2] = 0;

                    rotMat[2, 0] = -st;
                    rotMat[2, 1] = 0;
                    rotMat[2, 2] = ct;
                    break;
                case RotationAxis.AxisZ:
                    rotMat[0, 0] = ct;
                    rotMat[0, 1] = -st;
                    rotMat[0, 2] = 0;

                    rotMat[1, 0] = st;
                    rotMat[1, 1] = ct;
                    rotMat[1, 2] = 0;

                    rotMat[2, 0] = 0;
                    rotMat[2, 1] = 0;
                    rotMat[2, 2] = 1;
                    break;
            }

            return rotMat;
        }

        private static Quaternion rotMatrixToQuaternion(Matrix4x4 rotMatrix)
        {
            Quaternion q;
            float trace = rotMatrix[0, 0] + rotMatrix[1, 1] + rotMatrix[2, 2];
            if (trace > 0)
            {
                float s = 0.5f / (float) Math.Sqrt(trace + 1.0f);
                q.w = 0.25f / s;
                q.x = -(rotMatrix[2, 1] - rotMatrix[1, 2]) * s;
                q.y = -(rotMatrix[0, 2] - rotMatrix[2, 0]) * s;
                q.z = -(rotMatrix[1, 0] - rotMatrix[0, 1]) * s;
            }
            else
            {
                if (rotMatrix[0, 0] > rotMatrix[1, 1] && rotMatrix[0, 0] > rotMatrix[2, 2])
                {
                    float s = 0.5f / (float) Math.Sqrt(1.0f + rotMatrix[0, 0] - rotMatrix[1, 1] - rotMatrix[2, 2]);
                    q.w = -(rotMatrix[2, 1] - rotMatrix[1, 2]) * s;
                    q.x = 0.25f / s;
                    q.y = (rotMatrix[0, 1] + rotMatrix[1, 0]) * s;
                    q.z = (rotMatrix[0, 2] + rotMatrix[2, 0]) * s;
                }
                else if (rotMatrix[1, 1] > rotMatrix[2, 2])
                {
                    float s = 0.5f / (float) Math.Sqrt(1.0f + rotMatrix[1, 1] - rotMatrix[0, 0] - rotMatrix[2, 2]);
                    q.w = -(rotMatrix[0, 2] - rotMatrix[2, 0]) * s;
                    q.x = (rotMatrix[0, 1] + rotMatrix[1, 0]) * s;
                    q.y = 0.25f / s;
                    q.z = (rotMatrix[1, 2] + rotMatrix[2, 1]) * s;
                }
                else
                {
                    float s = 0.5f / (float) Math.Sqrt(1.0f + rotMatrix[2, 2] - rotMatrix[0, 0] - rotMatrix[1, 1]);
                    q.w = -(rotMatrix[1, 0] - rotMatrix[0, 1]) * s;
                    q.x = (rotMatrix[0, 2] + rotMatrix[2, 0]) * s;
                    q.y = (rotMatrix[1, 2] + rotMatrix[2, 1]) * s;
                    q.z = 0.25f / s;
                }
            }

            return q;
        }

        private static Matrix4x4 quaternionToRotMatrix(Quaternion quaternion)
        {
            Matrix4x4 rotMatrix = Matrix4x4.identity;

            rotMatrix[0, 0] = 1 - 2 * quaternion.y * quaternion.y - 2 * quaternion.z * quaternion.z;
            rotMatrix[0, 1] = 2 * quaternion.x * quaternion.y + 2 * quaternion.w * quaternion.z;
            rotMatrix[0, 2] = 2 * quaternion.x * quaternion.z - 2 * quaternion.w * quaternion.y;

            rotMatrix[1, 0] = 2 * quaternion.x * quaternion.y - 2 * quaternion.w * quaternion.z;
            rotMatrix[1, 1] = 1 - 2 * quaternion.x * quaternion.x - 2 * quaternion.z * quaternion.z;
            rotMatrix[1, 2] = 2 * quaternion.y * quaternion.z + 2 * quaternion.w * quaternion.x;

            rotMatrix[2, 0] = 2 * quaternion.x * quaternion.z + 2 * quaternion.w * quaternion.y;
            rotMatrix[2, 1] = 2 * quaternion.y * quaternion.z - 2 * quaternion.w * quaternion.x;
            rotMatrix[2, 2] = 1 - 2 * quaternion.x * quaternion.x - 2 * quaternion.y * quaternion.y;

            return rotMatrix;
        }

        private static Vector3 rotMatrixToRollPitchYaw(Matrix4x4 rotMatrix) // roll (RX), pitch (RY), yaw (RZ) [rad]
        {
            Vector3 angles = Vector3.zero;
            double sy = Math.Sqrt(rotMatrix[0, 0] * rotMatrix[0, 0] + rotMatrix[1, 0] * rotMatrix[1, 0]);
            if (sy < 1e-6)
            {
                // singular
                angles.z = 0;
                angles.y = (float) Math.Atan2(-rotMatrix[2, 0], sy);
                angles.x = (float) Math.Atan2(-rotMatrix[1, 2], rotMatrix[1, 1]);
            }
            else
            {
                angles.z = (float) Math.Atan2(rotMatrix[1, 0], rotMatrix[0, 0]);
                angles.y = (float) Math.Atan2(-rotMatrix[2, 0], sy);
                angles.x = (float) Math.Atan2(rotMatrix[2, 1], rotMatrix[2, 2]);
            }

            return angles;
        }

        /// Function to convert a robot P-type variable to
        /// left-handed Unity reference frame
        /// (in)    robotPTypeVar               : double[]      --> (only the first 6 values are taken into account: X [mm], Y [mm], Z [mm], RX [deg], RY [deg], RZ [deg])
        /// (in)    unityInitialQuat (OPTIONAL) : Quaternion    --> unity quaternion describing the initial orientation of the gameobject (if not set, Identity)
        /// (out)   Pose: unity Pose data-type (position [m] + orientation)
         public static Pose RobotBase2UnityPose(double[] robotPTypeVar, Quaternion? unityInitialQuat = null, bool round = false)
        {
            // conversion into the Unity left-handed reference system
            Quaternion unityBaseQuat = new Quaternion();
            unityBaseQuat.eulerAngles = new Vector3(90.0f, 180.0f, 0.0f);

            Quaternion unityBaseQuaternion = unityInitialQuat ?? unityBaseQuat;
            Matrix4x4 robotMatrix = BuildRotationMatrix(robotPTypeVar[5] * Math.PI / 180.0f, RotationAxis.AxisZ)
                                    * BuildRotationMatrix(robotPTypeVar[4] * Math.PI / 180.0f, RotationAxis.AxisY)
                                    * BuildRotationMatrix(robotPTypeVar[3] * Math.PI / 180.0f, RotationAxis.AxisX);
            robotMatrix[0, 3] = (float) robotPTypeVar[0] / 1000.0f;
            robotMatrix[1, 3] = (float) robotPTypeVar[1] / 1000.0f;
            robotMatrix[2, 3] = (float) robotPTypeVar[2] / 1000.0f;

            // TODO: we need to roto-translate the robotMatrix according to the active robot WORK frame
            // e.g. robotMatrix = _workFrame * robotMatrix;
            //
            // for the moment let's assume robot P-type variables are always expressed in WORK0 (robot base)

            Quaternion unityQuat = rotMatrixToQuaternion(robotMatrix);
            unityQuat.z = -unityQuat.z;

            Pose unityPose = new Pose();
            unityPose.position = unityBaseQuaternion *
                                 new Vector3(robotMatrix[0, 3], robotMatrix[1, 3], -robotMatrix[2, 3]);
            unityPose.rotation = unityBaseQuaternion * unityQuat;

            if (round)
            {
                unityPose.position.x = (float) Math.Round(unityPose.position.x, 2);
                unityPose.position.y = (float) Math.Round(unityPose.position.y, 2);
                unityPose.position.z = (float) Math.Round(unityPose.position.z, 2);
                unityPose.rotation.x = (float) Math.Round(unityPose.rotation.x, 2);
                unityPose.rotation.y = (float) Math.Round(unityPose.rotation.y, 2);
                unityPose.rotation.z = (float) Math.Round(unityPose.rotation.z, 2);
                unityPose.rotation.w = (float) Math.Round(unityPose.rotation.w, 2);
            }
               

            return unityPose;
        }

        /// Function to convert a left-handed Unity reference frame
        /// to a robot P-type variable
        /// (in)   unityPose                    : Pose          --> unity Pose data-type (position [m] + orientation)
        /// (in)   unityInitialQuat (OPTIONAL)  : Quaternion    --> unity quaternion describing the initial orientation of the gameobject (if not set, Identity)
        /// (out)  double[] (7 values : X [mm], Y [mm], Z [mm], RX [deg], RY [deg], RZ [deg], FIG)
        /// 
        public static double[] UnityPose2RobotBase(Pose unityPose, Quaternion? unityInitialQuat = null, bool round = false)
        {
            // conversion into the Unity left-handed reference system
            Quaternion unityBaseQuat = new Quaternion();
            unityBaseQuat.eulerAngles = new Vector3(90.0f, 180.0f, 0.0f);

            Quaternion unityBaseQuaternion = unityInitialQuat ?? unityBaseQuat;

            Vector3 robotPosition = Quaternion.Inverse(unityBaseQuaternion) * unityPose.position;

            Quaternion unityQuat = Quaternion.Inverse(unityBaseQuaternion) * unityPose.rotation;
            unityQuat.z = -unityQuat.z;

            Matrix4x4 robotMatrix = quaternionToRotMatrix(unityQuat);
            robotMatrix[0, 3] = robotPosition.x;
            robotMatrix[1, 3] = robotPosition.y;
            robotMatrix[2, 3] = -robotPosition.z;

            // TODO: we need to roto-translate the unityMatrix according to the active robot WORK frame
            // e.g. robotMatrix = _workFrame.inverse * robotMatrix;
            //
            // for the moment let's assume robot P-type variables are always expressed in WORK0 (robot base)

            Vector3 robotAngles = rotMatrixToRollPitchYaw(robotMatrix);

            double[] robotPTypeVar = new double[7];
            robotPTypeVar[0] = robotMatrix[0, 3] * 1000.0f;
            robotPTypeVar[1] = robotMatrix[1, 3] * 1000.0f;
            robotPTypeVar[2] = robotMatrix[2, 3] * 1000.0f;

            robotPTypeVar[3] = robotAngles.x * 180.0f / Math.PI;
            robotPTypeVar[4] = robotAngles.y * 180.0f / Math.PI;
            robotPTypeVar[5] = robotAngles.z * 180.0f / Math.PI;

            // last element (FIG) is fixed to -1
            robotPTypeVar[6] = -1;

            if (round)
            {
                robotPTypeVar[0] = Math.Round(robotPTypeVar[0], 1);
                robotPTypeVar[1] = Math.Round(robotPTypeVar[1], 1);
                robotPTypeVar[2] = Math.Round(robotPTypeVar[2], 1);
                robotPTypeVar[3] = Math.Round(robotPTypeVar[3], 1);
                robotPTypeVar[4] = Math.Round(robotPTypeVar[4], 1);
                robotPTypeVar[5] = Math.Round(robotPTypeVar[5], 1);
            }

            return robotPTypeVar;
        }



        public static void UpdateTransformationTool(DensoInterface denso, int toolNumber, double[] toolDef)
        {
            // roto-translation matrix in the right-handed robot base reference system
            Matrix4x4 toolMatrix = DensoTools.BuildRotationMatrix(toolDef[5] * Math.PI / 180.0f, RotationAxis.AxisZ)
                                   * BuildRotationMatrix(toolDef[4] * Math.PI / 180.0f, RotationAxis.AxisY)
                                   * BuildRotationMatrix(toolDef[3] * Math.PI / 180.0f, RotationAxis.AxisX);
            toolMatrix[0, 3] = (float) toolDef[0] / 1000.0f;
            toolMatrix[1, 3] = (float) toolDef[1] / 1000.0f;
            toolMatrix[2, 3] = (float) toolDef[2] / 1000.0f;
            denso.ToolNumber = toolNumber;
            denso._toolFrame = toolMatrix;

            // conversion into the Unity left-handed reference system
            Quaternion unityBaseQuat = new Quaternion();
            unityBaseQuat.eulerAngles = new Vector3(180.0f, 0.0f, 0.0f);
            denso._robotTool = RobotBase2UnityPose(toolDef, unityBaseQuat);
        }

        public static void UpdateTransformationWork(DensoInterface denso, int workNumber, double[] workDef)
        {
            // roto-translation matrix in the right-handed robot base coordinates system
            Matrix4x4 workMatrix = BuildRotationMatrix(workDef[5] * Math.PI / 180.0f, RotationAxis.AxisZ)
                                   * BuildRotationMatrix(workDef[4] * Math.PI / 180.0f, RotationAxis.AxisY)
                                   * BuildRotationMatrix(workDef[3] * Math.PI / 180.0f, RotationAxis.AxisX);
            workMatrix[0, 3] = (float) workDef[0] / 1000.0f;
            workMatrix[1, 3] = (float) workDef[1] / 1000.0f;
            workMatrix[2, 3] = (float) workDef[2] / 1000.0f;
            denso.WorkNumber = workNumber;
            denso._workFrame = workMatrix;

            // conversion into the Unity left-handed reference system
            Quaternion unityBaseQuat = new Quaternion();
            unityBaseQuat.eulerAngles = new Vector3(90.0f, 180.0f, 0.0f);
            denso._robotWork = RobotBase2UnityPose(workDef, unityBaseQuat);
        }

        public static void UpdateTransformationArea(DensoInterface denso, int areaNumber, double[] areaDef)
        {
            // roto-translation matrix in the right-handed robot base coordinates system
            Matrix4x4 areaMatrix = BuildRotationMatrix(areaDef[5] * Math.PI / 180.0f, RotationAxis.AxisZ)
                                   * BuildRotationMatrix(areaDef[4] * Math.PI / 180.0f, RotationAxis.AxisY)
                                   * BuildRotationMatrix(areaDef[3] * Math.PI / 180.0f, RotationAxis.AxisX);
            areaMatrix[0, 3] = (float) areaDef[0] / 1000.0f;
            areaMatrix[1, 3] = (float) areaDef[2] / 1000.0f;
            areaMatrix[2, 3] = (float) areaDef[1] / 1000.0f;

            denso.Area.Num = areaNumber;
            denso.Area.Transform = areaMatrix;
            denso.Area.Size = new Vector3((float) areaDef[6] / 1000.0f, (float) areaDef[7] / 1000.0f,
                (float) areaDef[8] / 1000.0f);
            denso.Area.IOLine = (int) areaDef[9];
            denso.Area.PosVar = (int) areaDef[10];
            denso.Area.ErrorType = (int) areaDef[11];
            denso.Area.Enabled = (areaDef[33] == 1);

            // conversion into the Unity left-handed reference system
            Quaternion unityBaseQuat = new Quaternion();
            unityBaseQuat.eulerAngles = new Vector3(90.0f, 180.0f, 0.0f);
            denso.Area.UnityPose = RobotBase2UnityPose(areaDef, unityBaseQuat);
        }

        public static Pose UpdateTransformationSafetyArea(double[] safetyAreaDef, ref Matrix4x4 robotMatrix)
        {
            // roto-translation matrix in the right-handed robot base coordinates system
            Matrix4x4 safetyAreaMatrix = BuildRotationMatrix(safetyAreaDef[5] * Math.PI / 180.0f, RotationAxis.AxisZ)
                                         * BuildRotationMatrix(safetyAreaDef[4] * Math.PI / 180.0f, RotationAxis.AxisY)
                                         * BuildRotationMatrix(safetyAreaDef[3] * Math.PI / 180.0f, RotationAxis.AxisX);
            safetyAreaMatrix[0, 3] = (float) safetyAreaDef[0] / 1000.0f;
            safetyAreaMatrix[1, 3] = (float) safetyAreaDef[1] / 1000.0f;
            safetyAreaMatrix[2, 3] = (float) safetyAreaDef[2] / 1000.0f;

            robotMatrix = safetyAreaMatrix;

            // conversion into the Unity left-handed reference system
            Quaternion unityBaseQuat = new Quaternion();
            unityBaseQuat.eulerAngles = new Vector3(90.0f, 180.0f, 0.0f);
            return RobotBase2UnityPose(safetyAreaDef, unityBaseQuat);
        }

        public static bool ParseXML(DensoInterface denso)
        {
            denso.SafetyAreasList.Clear();
            denso.SafetyFencesList.Clear();
            if (denso.ConnectRealRobot)
            {
                Debug.LogWarning("[" + denso.gameObject.name +
                                 "] : Safety areas can be shown only when connected to VRC.\nPlease disconnect from real robot ...");
                return false;
            }

            string projectParentDirectory = Path.GetFileName(Path.GetDirectoryName(denso.WincapsProject));
            string robotSafetyDirectory = Path.Combine(Path.GetFileNameWithoutExtension(denso.WincapsProject),
                "Safety model data\\ArmModel.DW3");
            string robotSafetyDataFile =
                Path.Combine(Path.GetDirectoryName(denso.WincapsProject), robotSafetyDirectory);

            if (File.Exists(robotSafetyDataFile))
            {
                XmlDocument robotSafetyData = new XmlDocument();
                robotSafetyData.Load(robotSafetyDataFile);
                XmlElement outerNode = robotSafetyData.DocumentElement;
                XmlNode rootNode = outerNode.FirstChild;
                foreach (XmlNode node in rootNode.SelectNodes("INode"))
                {
                    XmlNode image = node.SelectSingleNode("Image");
                    if ((image.InnerText == "Node") || (image.InnerText == "Tobj"))
                    {
                        XmlNode clsDWNode = node.SelectSingleNode("clsDWNode");
                        XmlNode collisionProperty = clsDWNode.SelectSingleNode("m_bCollision");
                        if (collisionProperty.InnerText == "True")
                        {
                            RobotSafetyArea safetyArea = new RobotSafetyArea(-1);
                            double[] safetyAreaDefinition = new double[9];
                            Matrix4x4 robotTransformation = Matrix4x4.identity;
                            safetyAreaDefinition[0] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngOffsetX").InnerText);
                            safetyAreaDefinition[1] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngOffsetY").InnerText);
                            safetyAreaDefinition[2] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngOffsetZ").InnerText);
                            safetyAreaDefinition[3] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngOffsetRX").InnerText);
                            safetyAreaDefinition[4] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngOffsetRY").InnerText);
                            safetyAreaDefinition[5] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngOffsetRZ").InnerText);
                            safetyAreaDefinition[6] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngScaleX").InnerText) / 2;
                            safetyAreaDefinition[7] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngScaleY").InnerText) / 2;
                            safetyAreaDefinition[8] =
                                Double.Parse(clsDWNode.SelectSingleNode("m_sngScaleZ").InnerText) / 2;

                            safetyArea.Name = node.Attributes["Name"].Value;
                            safetyArea.UnityPose =
                                UpdateTransformationSafetyArea(safetyAreaDefinition, ref robotTransformation);
                            safetyArea.Transform = robotTransformation;

                            safetyArea.Size = new Vector3((float) safetyAreaDefinition[6] / 1000.0f,
                                (float) safetyAreaDefinition[7] / 1000.0f, (float) safetyAreaDefinition[8] / 1000.0f);
                            safetyArea.MonitoringIndex =
                                Int32.Parse(clsDWNode.SelectSingleNode("m_lVirtualFenceControlID").InnerText);

                            denso.SafetyAreasList.Add(safetyArea);
                        }
                        else
                        {
                            foreach (XmlNode fence in node.SelectNodes("INode"))
                            {
                                if (fence.SelectSingleNode("Image").InnerText == "FenceObject")
                                {
                                    XmlNode clsDWNodeFence = fence.SelectSingleNode("clsDWNode");
                                    if (clsDWNodeFence != null)
                                    {
                                        RobotSafetyArea safetyFence = new RobotSafetyArea(-1);
                                        double[] safetyFenceDefinition = new double[9];
                                        Matrix4x4 robotTransformation = Matrix4x4.identity;
                                        safetyFenceDefinition[0] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngOffsetX").InnerText);
                                        safetyFenceDefinition[1] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngOffsetY").InnerText);
                                        safetyFenceDefinition[2] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngOffsetZ").InnerText);
                                        safetyFenceDefinition[3] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngOffsetRX").InnerText);
                                        safetyFenceDefinition[4] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngOffsetRY").InnerText);
                                        safetyFenceDefinition[5] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngOffsetRZ").InnerText);
                                        safetyFenceDefinition[6] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngScaleX").InnerText) / 2;
                                        safetyFenceDefinition[7] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngScaleY").InnerText) / 2;
                                        safetyFenceDefinition[8] =
                                            Double.Parse(clsDWNodeFence.SelectSingleNode("m_sngScaleZ").InnerText) / 2;

                                        Matrix4x4 rotMat =
                                            BuildRotationMatrix(safetyFenceDefinition[5] * Math.PI / 180.0f,
                                                RotationAxis.AxisZ)
                                            * BuildRotationMatrix(safetyFenceDefinition[4] * Math.PI / 180.0f,
                                                RotationAxis.AxisY)
                                            * BuildRotationMatrix(safetyFenceDefinition[3] * Math.PI / 180.0f,
                                                RotationAxis.AxisX);
                                        Vector3 offset = rotMat.MultiplyPoint3x4(
                                            new Vector3((float) safetyFenceDefinition[6],
                                                (float) safetyFenceDefinition[7], (float) safetyFenceDefinition[8]));
                                        safetyFenceDefinition[0] += offset.x;
                                        safetyFenceDefinition[1] += offset.y;
                                        safetyFenceDefinition[2] -= offset.z;

                                        safetyFence.Name = node.Attributes["Name"].Value;
                                        safetyFence.UnityPose = UpdateTransformationSafetyArea(safetyFenceDefinition,
                                            ref robotTransformation);
                                        safetyFence.Transform = robotTransformation;
                                        safetyFence.Size = new Vector3((float) safetyFenceDefinition[6] / 1000.0f,
                                            (float) safetyFenceDefinition[7] / 1000.0f,
                                            (float) safetyFenceDefinition[8] / 1000.0f);
                                        safetyFence.MonitoringIndex = Int32.Parse(clsDWNode
                                            .SelectSingleNode("m_lVirtualFenceControlID").InnerText);

                                        denso.SafetyFencesList.Add(safetyFence);
                                    }
                                    else
                                    {
                                        Debug.LogWarning("[" + denso.gameObject.name +
                                                         "] : Unexpected formatting for Fence objects ...");
                                    }
                                }
                            }
                        }
                    }
                }

                if (denso.SafetyAreasList.Count == 0)
                {
                    Debug.LogWarning("[" + denso.name +
                                     "] : Safety data are present but no safety area has been defined ...");
                }

                if (denso.SafetyFencesList.Count == 0)
                {
                    Debug.LogWarning("[" + denso.name +
                                     "] : Safety data are present but no safety fences have been defined ...");
                }

                return true;
            }
            else
            {
                Debug.LogWarning("[" + denso.name + "] : No valid safety data can be found ...");
                return false;
            }
        }


        public static void DrawLine(Vector3 p1, Vector3 p2)
        {
            GL.Vertex(p1);
            GL.Vertex(p2);
        }
    }
}