using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Color4 = Silk.NET.Maths.Vector4D<float>;

namespace DynamicCodeSurfaceDemo
{
    public class MainViewModel : BaseViewModel
    {
        private double parameterW = 1;
        public double ParameterW
        {
            set => SetValue(ref parameterW, value);
            get => parameterW;
        }
        private int meshSizeU = 120;
        public int MeshSizeU
        {
            set => SetValue(ref meshSizeU, value);
            get => meshSizeU;
        }

        private int meshSizeV = 120;
        public int MeshSizeV
        {
            set => SetValue(ref meshSizeV, value);
            get => meshSizeV;
        }

        private Material material;
        public Material Material
        {
            set => SetValue(ref material, value);
            get => material;
        }

        public string[] Materials { private set; get; }

        public string[] Models { private set; get; }

        private List<Uri> sourceCodeUri { get; } = new List<Uri>();
        private Dictionary<string, string> fileDict = new Dictionary<string, string>();
        private Dictionary<string, Material> materialDict = new Dictionary<string, Material>();

        private string selectedModel;
        public string SelectedModel
        {
            set
            {
                if (SetValue(ref selectedModel, value))
                {
                    LoadSourceCode();
                }
            }
            get => selectedModel;
        }
        private string selectedMaterial;
        public string SelectedMaterial
        {
            set
            {
                if (SetValue(ref selectedMaterial, value))
                {
                    Material = materialDict[value];
                }
            }
            get => selectedMaterial;
        }
        private string sourceCode;
        public string SourceCode
        {
            set
            {
                SetValue(ref sourceCode, value);
            }
            get => sourceCode;
        }

        public MainViewModel()
        {
            EffectsManager = new DefaultEffectsManager();
            Camera = new OrthographicCamera() { Position = new System.Windows.Media.Media3D.Point3D(0, 0, -10),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 10),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FarPlaneDistance = 2000, NearPlaneDistance = 1
            };
            //foreach (string m in Models)
            //{
            //    var uri = new Uri(String.Format("pack://application:,,/Expressions/{0}.txt", m));
            //    sourceCodeUri.Add(uri);
            //}

            var dir = "Expressions";
            if (!Directory.Exists(dir))
                return;
            string[] files = Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                fileDict.Add(Path.GetFileNameWithoutExtension(file), Path.GetFullPath(file));
            }
            Models = fileDict.Keys.ToArray();

            materialDict.Add("Normal", new NormalMaterial());
            materialDict.Add("Position", new PositionColorMaterial());
            materialDict.Add("Copper", PhongMaterials.Copper);
            materialDict.Add("Chrome", PhongMaterials.Chrome);
            materialDict.Add("BlackRubber", PhongMaterials.BlackRubber);
            materialDict.Add("Pearl", PhongMaterials.Pearl);
            materialDict.Add("PolishedBronze", PhongMaterials.PolishedBronze);
            materialDict.Add("ColorStripe", new ColorStripeMaterial() { ColorStripeX = GetGradients(new Color4(1, 0, 0, 1), new Color4(0, 1, 0, 1), new Color4(0, 0, 1, 1), 48).ToArray() });
            materialDict.Add("Diffuse", DiffuseMaterials.Orange);
            Materials = materialDict.Keys.ToArray();
            SelectedMaterial = "Normal";
        }

        void LoadSourceCode()
        {
            if(fileDict.TryGetValue(selectedModel, out string filePath))
            {
                using (var reader = File.OpenRead(filePath))
                {
                    using(var strReader = new StreamReader(reader))
                    {
                        SourceCode = strReader.ReadToEnd();
                    }
                }
            }
        }

        public static IEnumerable<Color4> GetGradients(Color4 start, Color4 mid, Color4 end, int steps)
        {
            return GetGradients(start, mid, steps / 2).Concat(GetGradients(mid, end, steps / 2));
        }

        public static IEnumerable<Color4> GetGradients(Color4 start, Color4 end, int steps)
        {
            float stepA = ((end.W - start.W) / (steps - 1));
            float stepR = ((end.X - start.X) / (steps - 1));
            float stepG = ((end.Y - start.Y) / (steps - 1));
            float stepB = ((end.Z - start.Z) / (steps - 1));

            for (int i = 0; i < steps; i++)
            {
                yield return new Color4((start.X + (stepR * i)),
                                            (start.Y + (stepG * i)),
                                            (start.Z + (stepB * i)),
                                            (start.W + (stepA * i)));
            }
        }
    }
}
