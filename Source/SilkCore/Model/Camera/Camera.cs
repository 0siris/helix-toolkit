/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Globalization;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core {
    namespace Cameras {
        public abstract class CameraCore : ObservableObject, ICamera {
            private float accumTime;
            private float aniTime;

            private bool createLeftHandSystem;

            private Vector3 lookDirection;
            private Vector3 oldLookDir;
            private Vector3 oldPosition;
            private Vector3 oldUpDir;
            private Vector3 position;
            private long prevTicks;
            private Vector3 targetLookDirection;


            private Vector3 targetPosition;
            private Vector3 targetUpDirection;

            private Vector3 upDirection;

            public Vector3 Target => position + lookDirection;

            public Vector3 Position {
                get => position;
                set => Set(ref position, value);
            }

            public Vector3 LookDirection {
                get => lookDirection;
                set => Set(ref lookDirection, value);
            }

            public Vector3 UpDirection {
                get => upDirection;
                set => Set(ref upDirection, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether to create a left hand system.
            /// </summary>
            /// <value>
            ///     <c>true</c> if creating a left hand system; otherwise, <c>false</c>.
            /// </value>
            public bool CreateLeftHandSystem {
                get => createLeftHandSystem;
                set => Set(ref createLeftHandSystem, value);
            }

            public abstract Matrix CreateProjectionMatrix(float aspectRatio);

            public abstract Matrix CreateProjectionMatrix(float aspectRatio, float nearPlane, float farPlane);

            public abstract Matrix CreateViewMatrix();

            public abstract FrustumCameraParams CreateCameraParams(float aspectRatio);
            public abstract FrustumCameraParams CreateCameraParams(float aspectRatio, float nearPlane, float farPlane);

            public override string ToString() {
                var target = Position + LookDirection;
                return string.Format(CultureInfo.InvariantCulture,
                                     "LookDirection:\t{0:0.000},{1:0.000},{2:0.000}",
                                     LookDirection.X,
                                     LookDirection.Y,
                                     LookDirection.Z) + "\n"
                                                      + string.Format(CultureInfo.InvariantCulture,
                                                                      "UpDirection:\t{0:0.000},{1:0.000},{2:0.000}",
                                                                      UpDirection.X,
                                                                      UpDirection.Y,
                                                                      UpDirection.Z) + "\n"
                                                      + string.Format(CultureInfo.InvariantCulture,
                                                                      "Position:\t\t{0:0.000},{1:0.000},{2:0.000}",
                                                                      Position.X,
                                                                      Position.Y,
                                                                      Position.Z) + "\n"
                                                      + string.Format(CultureInfo.InvariantCulture,
                                                                      "Target:\t\t{0:0.000},{1:0.000},{2:0.000}",
                                                                      target.X,
                                                                      target.Y,
                                                                      target.Z);
            }

            /// <summary>
            ///     Animates to.
            /// </summary>
            /// <param name="newPosition">The new position.</param>
            /// <param name="newDirection">The new direction.</param>
            /// <param name="newUpDirection">The new up direction.</param>
            /// <param name="animationTime">The animation time.</param>
            public void AnimateTo(
                Vector3 newPosition,
                Vector3 newDirection,
                Vector3 newUpDirection,
                float animationTime
            ) {
                if (animationTime == 0) {
                    Position = newPosition;
                    LookDirection = newDirection;
                    UpDirection = newUpDirection;
                    aniTime = 0;
                } else {
                    targetPosition = newPosition;
                    targetLookDirection = newDirection;
                    targetUpDirection = newUpDirection;
                    oldPosition = Position;
                    oldLookDir = LookDirection;
                    oldUpDir = UpDirection;
                    aniTime = animationTime;
                    accumTime = 1;
                    prevTicks = Stopwatch.GetTimestamp();
                    OnUpdateAnimation(0);
                }
            }

            /// <summary>
            ///     Called when [time step] to update camera animation.
            /// </summary>
            /// <returns></returns>
            public virtual bool OnTimeStep() {
                var ticks = Stopwatch.GetTimestamp();
                var ellapsed = (float) (ticks - prevTicks) / Stopwatch.Frequency * 1000;
                prevTicks = ticks;
                return OnUpdateAnimation(ellapsed);
            }

            protected virtual bool OnUpdateAnimation(float ellapsed) {
                if (aniTime == 0) return false;
                accumTime += ellapsed;
                if (accumTime > aniTime) {
                    Position = targetPosition;
                    LookDirection = targetLookDirection;
                    UpDirection = targetUpDirection;
                    aniTime = 0;
                    return false;
                }

                var l = accumTime / aniTime;
                var nextPos = SilkMath.Lerp(oldPosition, targetPosition, l);
                var nextLook = SilkMath.Lerp(oldLookDir, targetLookDirection, l);
                var nextUp = SilkMath.Lerp(oldUpDir, targetUpDirection, l);
                Position = nextPos;
                LookDirection = nextLook;
                UpDirection = nextUp;
                return true;
            }

            public void StopAnimation() {
                aniTime = 0;
            }
        }

        public abstract class ProjectionCameraCore : CameraCore {
            private float farPlane = 100;

            private float nearPlane = 0.001f;

            /// <summary>
            ///     Gets or sets the far plane distance.
            /// </summary>
            /// <value>
            ///     The far plane distance.
            /// </value>
            public float FarPlaneDistance {
                get => farPlane;
                set => Set(ref farPlane, value);
            }

            /// <summary>
            ///     Gets or sets the near plane distance.
            /// </summary>
            /// <value>
            ///     The near plane distance.
            /// </value>
            public float NearPlaneDistance {
                get => nearPlane;
                set => Set(ref nearPlane, value);
            }

            public override Matrix CreateViewMatrix() {
                return CreateLeftHandSystem
                           ? SilkMath.LookAtLH(Position, Position + LookDirection, UpDirection)
                           : SilkMath.LookAtRH(Position, Position + LookDirection, UpDirection);
            }

            public override string ToString() {
                return base.ToString() + "\n" +
                       string.Format(CultureInfo.InvariantCulture, "NearPlaneDist:\t{0}", NearPlaneDistance) + "\n"
                       + string.Format(CultureInfo.InvariantCulture, "FarPlaneDist:\t{0}", FarPlaneDistance);
            }
        }

        public class OrthographicCameraCore : ProjectionCameraCore {
            private float width = 100;

            public float Width {
                get => width;
                set => Set(ref width, value);
            }

            public override FrustumCameraParams CreateCameraParams(float aspectRatio) {
                return CreateCameraParams(aspectRatio, NearPlaneDistance, FarPlaneDistance);
            }

            public override FrustumCameraParams CreateCameraParams(float aspectRatio, float nearPlane, float farPlane) {
                return new FrustumCameraParams {
                    AspectRatio = aspectRatio,
                    FOV = (float) Math.PI / 2,
                    LookAtDir = LookDirection,
                    UpDir = UpDirection,
                    Position = Position,
                    ZNear = nearPlane,
                    ZFar = farPlane
                };
            }

            public override Matrix CreateProjectionMatrix(float aspectRatio) {
                return CreateProjectionMatrix(aspectRatio, NearPlaneDistance, FarPlaneDistance);
            }

            public override Matrix CreateProjectionMatrix(float aspectRatio, float nearPlane, float farPlane) {
                return CreateLeftHandSystem
                           ? SilkMath.OrthoLH(Width, Width / aspectRatio, nearPlane, Math.Min(1e15f, farPlane))
                           : SilkMath.OrthoRH(Width, Width / aspectRatio, nearPlane, Math.Min(1e15f, farPlane));
            }


            public override string ToString() {
                return base.ToString() + "\n" + string.Format(CultureInfo.InvariantCulture, "Width:\t{0:0.###}", Width);
            }

#if CORE
            private float oldWidth;
            private float targetWidth;
            private float accumTime;
            private float aniTime;

            public void AnimateWidth(float newWidth, float animationTime) {
                if (animationTime == 0) {
                    UpdateCameraPositionByWidth(newWidth);
                    Width = newWidth;
                } else {
                    oldWidth = Width;
                    targetWidth = newWidth;
                    accumTime = 1;
                    aniTime = animationTime;
                    OnUpdateAnimation(0);
                }
            }

            protected override bool OnUpdateAnimation(float ellapsed) {
                var res = base.OnUpdateAnimation(ellapsed);
                if (aniTime == 0) return res;
                accumTime += ellapsed;
                if (accumTime > aniTime) {
                    UpdateCameraPositionByWidth(targetWidth);
                    Width = targetWidth;
                    aniTime = 0;
                    return res;
                }

                var newWidth = oldWidth + (targetWidth - oldWidth) * (accumTime / aniTime);
                UpdateCameraPositionByWidth(newWidth);
                Width = newWidth;
                return true;
            }

            private void UpdateCameraPositionByWidth(double newWidth) {
                var ratio = newWidth / Width;
                var dir = LookDirection;
                var target = Target;
                var dist = dir.Length;
                var newDist = dist * ratio;
                dir.Normalize();
                var position = target - dir * (float) newDist;
                var lookDir = dir * (float) newDist;
                Position = position;
                LookDirection = lookDir;
            }
#endif
        }

        public class PerspectiveCameraCore : ProjectionCameraCore {
            public float FieldOfView { get; set; } = 45;

            public override Matrix CreateProjectionMatrix(float aspectRatio) {
                return CreateProjectionMatrix(aspectRatio, NearPlaneDistance, FarPlaneDistance);
            }

            public override Matrix CreateProjectionMatrix(float aspectRatio, float nearPlane, float farPlane) {
                var fov = FieldOfView * Math.PI / 180;
                Matrix projM;
                if (CreateLeftHandSystem)
                    projM = SilkMath.PerspectiveFovLH((float) fov, aspectRatio, nearPlane, farPlane);
                else
                    projM = SilkMath.PerspectiveFovRH((float) fov, aspectRatio, nearPlane, farPlane);
                if (float.IsNaN(projM.M33) || float.IsNaN(projM.M43)) projM.M33 = projM.M43 = -1;
                return projM;
            }

            public override FrustumCameraParams CreateCameraParams(float aspectRatio) {
                return CreateCameraParams(aspectRatio, NearPlaneDistance, FarPlaneDistance);
            }

            public override FrustumCameraParams CreateCameraParams(float aspectRatio, float nearPlane, float farPlane) {
                return new FrustumCameraParams {
                    AspectRatio = aspectRatio,
                    FOV = FieldOfView / 180f * (float) Math.PI,
                    LookAtDir = LookDirection,
                    UpDir = UpDirection,
                    Position = Position,
                    ZNear = nearPlane,
                    ZFar = farPlane
                };
            }

            public override string ToString() {
                return base.ToString() + "\n" +
                       string.Format(CultureInfo.InvariantCulture, "FieldOfView:\t{0:0.#}°", FieldOfView);
            }
        }
    }
}
