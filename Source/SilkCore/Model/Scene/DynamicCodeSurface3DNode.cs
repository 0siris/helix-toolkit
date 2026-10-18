/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using System.Reflection;
#if !WINDOWS_UWP
using Microsoft.CSharp;
using System.CodeDom.Compiler;
#endif

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public static class DynamicCodeSurfaceTemplate {
    public const string Template =
        @"
    using System;

    namespace MyNamespace {
    public class MyEvaluator {
    const double pi = Math.PI;
    public double cos(double x) { return Math.Cos(x); }
    public double sin(double x) { return Math.Sin(x); }
    public double abs(double x) { return Math.Abs(x); }
    public double sqrt(double x) { return Math.Sqrt(x); }
    public double sign(double x) { return Math.Sign(x); }
    public double sqr(double x) { return x*x; }
    public double log(double x) { return Math.Log(x); }
    public double exp(double x) { return Math.Exp(x); }
    public double pow(double x, double y) { return Math.Pow(x,y); }
    public Tuple<double,double,double,double> Evaluate(double u, double v, double w) {
    double x=0,y=0,z=0;
    double color=u;
    #code#
    return new Tuple<double,double,double,double>(x,y,z,color);
    }
    }
    }";
}
#if !WINDOWS_UWP
/// <summary>
/// </summary>
public class DynamicCodeSurface3DNode : ParametricSurface3DNode {
    private object? codeInstance;

    // Type and instance of the dynamic code
    private Type? codeType;

    private string? sourceCode;

    public float ParameterW {
        get;
        set {
            if (Set(ref field, value)) UpdateSource();
        }
    } = 1f;

    /// <summary>
    ///     Gets or sets the source code
    /// </summary>
    /// <value>
    ///     The source.
    /// </value>
    public string? Source {
        get;
        set {
            if (Set(ref field, value)) UpdateSource();
        }
    }

    public CompilerErrorCollection? Errors {
        get;
        private set {
            if (Set(ref field, value)) OnCompileError?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? OnCompileError;

    private void UpdateSource() {
        sourceCode = Source;
        codeType = null;
        codeInstance = null;
        if (string.IsNullOrEmpty(sourceCode))
            return;

        var provider = new CSharpCodeProvider();
        var options = new CompilerParameters { GenerateInMemory = true };
        var qn = typeof(Vector3).Assembly.Location;
        options.ReferencedAssemblies.Add("System.dll");
        options.ReferencedAssemblies.Add(qn);
        var src = GetTemplate().Replace("#code#", sourceCode);
        var compilerResults = provider.CompileAssemblyFromSource(options, src);
        if (!compilerResults.Errors.HasErrors) {
            Errors = null;
            if (compilerResults.CompiledAssembly?.CreateInstance("MyNamespace.MyEvaluator") is not { } instance)
                return;

            codeInstance = instance;
            codeType = instance.GetType();
            TessellateAsync();
        } else {
            // correct line numbers
            Errors = compilerResults.Errors;
            if (Errors is { } errors)
                for (var i = 0; i < errors.Count; i++)
                    errors[i].Line -= 17;
        }
    }

    protected virtual string GetTemplate() => DynamicCodeSurfaceTemplate.Template;

    protected override Vector3 Evaluate(double u, double v, out Vector2 texCoord) {
        if (codeType is not { } type || codeInstance is not { } instance) {
            texCoord = new Vector2();
            return Vector3.Zero;
        }

        var parameters = new object[3];
        parameters[0] = u;
        parameters[1] = v;
        parameters[2] = ParameterW;
        var result = type.InvokeMember("Evaluate",
                                            BindingFlags.InvokeMethod,
                                            null,
                                            instance,
                                            parameters);
        if (result is not Tuple<double, double, double, double> p) {
            texCoord = new Vector2((float)u, (float)v);
            return Vector3.Zero;
        }

        // todo: why doesn't this work??
        //            texCoord = new Point(p.W, 0); // (double)parameters[2], 0);
        texCoord = new Vector2((float)u, (float)v);
        return new Vector3((float)p.Item1, (float)p.Item2, (float)p.Item3);
    }
}
#endif
