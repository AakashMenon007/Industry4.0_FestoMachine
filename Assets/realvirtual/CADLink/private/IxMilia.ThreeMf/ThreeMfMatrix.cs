// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System.Xml.Linq;

using UnityEngine;
using System.Globalization;


namespace ThreeMf
{
    public struct ThreeMfMatrix
    {
        internal const string TransformAttributeName = "transform";

        // represents a transform matrix of the form:
        //
        // [ M00 M01 M02 0.0 ]
        // [ M10 M11 M12 0.0 ]
        // [ M20 M21 M22 0.0 ]
        // [ M30 M31 M32 1.0 ]

        public double M00 { get; set; }
        public double M01 { get; set; }
        public double M02 { get; set; }

        public double M10 { get; set; }
        public double M11 { get; set; }
        public double M12 { get; set; }

        public double M20 { get; set; }
        public double M21 { get; set; }
        public double M22 { get; set; }

        public double M30 { get; set; }
        public double M31 { get; set; }
        public double M32 { get; set; }

        public bool IsIdentity =>
            M00 == 1.0 &&
            M01 == 0.0 &&
            M02 == 0.0 &&
            M10 == 0.0 &&
            M11 == 1.0 &&
            M12 == 0.0 &&
            M20 == 0.0 &&
            M21 == 0.0 &&
            M22 == 1.0 &&
            M30 == 0.0 &&
            M31 == 0.0 &&
            M32 == 0.0;

        public ThreeMfMatrix(double m00, double m01, double m02, double m10, double m11, double m12, double m20,
            double m21, double m22, double m30, double m31, double m32)
            : this()
        {
            M00 = m00;
            M01 = m01;
            M02 = m02;
            M10 = m10;
            M11 = m11;
            M12 = m12;
            M20 = m20;
            M21 = m21;
            M22 = m22;
            M30 = m30;
            M31 = m31;
            M32 = m32;
        }

        public Matrix4x4 GetTRS()
        {
            Matrix4x4 mat = new Matrix4x4(new Vector4((float) M00, (float) M10, (float) M20, (float) M30),
                new Vector4((float) M01, (float) M11, (float) M21, (float) M31),
                new Vector4((float) M02, (float) M12, (float) M22, (float) M32),
                new Vector4(0, 0, 0, 1));

            var convert = mat;
            return convert;
        }

        public Matrix4x4 GetTRSFlipped()
        {
            Matrix4x4 mat = new Matrix4x4(new Vector4((float) M00, (float) M10, (float) M20, (float) M30),
                new Vector4((float) M01, (float) M11, (float) M21, (float) M31),
                new Vector4((float) M02, (float) M12, (float) M22, (float) M32),
                new Vector4(0, 0, 0, 1));

            Matrix4x4 invert = new Matrix4x4(new Vector4(-1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0),
                new Vector4(0, 0, 0, 1));

            var convert = invert * mat * invert;
            return convert;
        }

        public static Vector3 GetAngles(Matrix4x4 source)
        {
            double thetaX, thetaY, thetaZ = 0.0;
            thetaX = System.Math.Asin(source.m21);
            var pi = System.Math.PI;
            if (thetaX < (pi / 2))
            {
                if (thetaX > (-pi / 2))
                {
                    thetaZ = System.Math.Atan2(-source.m01, source.m11);
                    thetaY = System.Math.Atan2(-source.m20, source.m22);
                }
                else
                {
                    thetaZ = -System.Math.Atan2(-source.m02, source.m00);
                    thetaY = 0;
                }
            }
            else
            {
                thetaZ = System.Math.Atan2(source.m02, source.m00);
                thetaY = 0;
            }

            float gradx = (float) ((180 / pi) * thetaX);
            float grady = (float) ((180 / pi) * thetaY);
            float gradz = (float) ((180 / pi) * thetaZ);
            // Create return object.

            // Return angles.
            return new Vector3(gradx, grady, gradz);
        }

        public Vector3 GetPosition()
        {
            var mat = GetTRSFlipped();
            Vector3 vect = new Vector3(mat.m30, mat.m31, mat.m32);

            return vect;
        }


        public Quaternion GetRotation()
        {
            var mat = GetTRS();
            return Quaternion.LookRotation(
                mat.GetColumn(1),
                mat.GetColumn(2)
            );
        }


        public Vector3 Get3mfAngles()
        {
            var mat = GetTRS();
            var angle = GetAngles(mat);

            return angle;
        }


        public Quaternion GetRotationQuaternion()
        {
            var mat = GetTRS();
            var angle = GetAngles(mat);
            var matorig = GetTRS();
            var angeorig = GetAngles(matorig);

            Quaternion qx = Quaternion.AngleAxis(-angle.x, new Vector3(1, 0, 0));
            Quaternion qy = Quaternion.AngleAxis(angle.y, new Vector3(0, 1, 0));
            Quaternion qz = Quaternion.AngleAxis(angle.z, new Vector3(0, 0, 1));
            Quaternion unityRotationQuaternion =qy * qx * qz; // exact order!

            return unityRotationQuaternion;
        }

        public Vector3 GetScale()
        {
            var mat = GetTRS();
            Vector3 scale = new Vector3(
                new Vector3((float) M00, (float) M01, (float) M02).magnitude,
                new Vector3((float) M10, (float) M11, (float) M12).magnitude,
                new Vector3((float) M20, (float) M21, (float) M22).magnitude
            );
            return scale;
        }

        public ThreeMfVertex Transform(ThreeMfVertex v)
        {
            //                       [ M00 M01 M02 0.0 ]
            // [ v.X v.Y v.Z 1.0 ] * [ M10 M11 M12 0.0 ]
            //                       [ M20 M21 M22 0.0 ]
            //                       [ M30 M31 M32 1.0 ]

            var x = v.X * M00 + v.Y * M10 + v.Z * M20 + M30;
            var y = v.Y * M01 + v.Y * M11 + v.Z * M21 + M31;
            var z = v.Z * M02 + v.Y * M12 + v.Z * M22 + M32;
            return new ThreeMfVertex(x, y, z);
        }

        internal XAttribute ToXAttribute()
        {
            if (IsIdentity)
            {
                return null;
            }

            var values = new[] {M00, M01, M02, M10, M11, M12, M20, M21, M22, M30, M31, M32};
            return new XAttribute(TransformAttributeName, string.Join(" ", values));
        }

        internal static ThreeMfMatrix ParseMatrix(XAttribute attribute)
        {
            if (attribute == null)
            {
                return Identity;
            }

            var parts = attribute.Value.Split(' ');
            if (parts.Length != 12)
            {
                throw new ThreeMfParseException("Expected 12 parts to parse transform matrix.");
            }

            double[] values = new double[12];
            var i = 0;
            foreach (var part in parts)
            {
                double.TryParse(part, NumberStyles.Any, new CultureInfo("en-US"), out var value);
                values[i] = value;
                i++;
            }

            return new ThreeMfMatrix(values[0], values[1], values[2], values[3], values[4], values[5], values[6],
                values[7], values[8], values[9], values[10], values[11]);
        }

        public static ThreeMfMatrix Identity =>
            new ThreeMfMatrix(1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0);

        public static bool operator ==(ThreeMfMatrix a, ThreeMfMatrix b)
        {
            if (ReferenceEquals(a, b))
                return true;
            return a.M00 == b.M00
                   && a.M01 == b.M01
                   && a.M02 == b.M02
                   && a.M10 == b.M10
                   && a.M11 == b.M11
                   && a.M12 == b.M12
                   && a.M20 == b.M20
                   && a.M21 == b.M21
                   && a.M22 == b.M22
                   && a.M30 == b.M30
                   && a.M31 == b.M31
                   && a.M32 == b.M32;
        }

        public static bool operator !=(ThreeMfMatrix a, ThreeMfMatrix b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return M00.GetHashCode()
                   ^ M01.GetHashCode()
                   ^ M02.GetHashCode()
                   ^ M10.GetHashCode()
                   ^ M11.GetHashCode()
                   ^ M12.GetHashCode()
                   ^ M20.GetHashCode()
                   ^ M21.GetHashCode()
                   ^ M22.GetHashCode()
                   ^ M30.GetHashCode()
                   ^ M31.GetHashCode()
                   ^ M32.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ThreeMfMatrix)
                return this == (ThreeMfMatrix) obj;
            return false;
        }
    }
}