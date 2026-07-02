using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using HelixToolkit.Wpf.SharpDX;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace CustomViewCubeDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MeshGeometryModel3D_Mouse3DDown(object sender, MouseDown3DEventArgs e)
        {
            var normal = Normalize(e.HitTestResult.NormalAtHit);
            var upDirection = Vector3.Zero;
            var lookDirection = -normal;
            if (Cross(normal, view1.ModelUpDirection.ToVector3()).LengthSquared < 1e-5)
            {
                var vecLeft = new Vector3(-normal.Y, -normal.Z, -normal.X);
                upDirection = vecLeft;
            }
            else
            {
                upDirection = view1.ModelUpDirection.ToVector3();
            }

            var target = view1.Camera.Position + view1.Camera.LookDirection;
            var distance = view1.Camera.LookDirection.Length;
            lookDirection *= (float)distance;
            var newPosition = target.ToVector3() - lookDirection;
            view1.Camera.AnimateTo(newPosition.ToPoint3D(), lookDirection.ToVector3D(), upDirection.ToVector3D(), 500);
        }

        private void LineGeometryModel3D_Mouse3DDown(object sender, MouseDown3DEventArgs e)
        {
            Debug.WriteLine("Line hitted.");
        }

        private static Vector3 Normalize(Vector3 value)
        {
            return value.Length > 0 ? value / value.Length : value;
        }

        private static Vector3 Cross(Vector3 left, Vector3 right)
        {
            return new Vector3(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }
    }
}
