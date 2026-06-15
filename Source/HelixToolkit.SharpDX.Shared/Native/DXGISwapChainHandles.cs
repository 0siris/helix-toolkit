/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    public enum Usage
    {
        RenderTargetOutput = 0x20
    }

    public enum SwapEffect
    {
        Discard = 0,
        Sequential = 1,
        FlipSequential = 3,
        FlipDiscard = 4
    }

    public enum Scaling
    {
        Stretch = 0,
        None = 1,
        AspectRatioStretch = 2
    }

    [Flags]
    public enum SwapChainFlags
    {
        None = 0,
        AllowModeSwitch = 2
    }

    [Flags]
    public enum PresentFlags
    {
        None = 0,
        Restart = 4
    }

    public struct ModeDescription
    {
        public Format Format;
    }

    public struct SwapChainDescription
    {
        public ModeDescription ModeDescription;
        public SwapChainFlags Flags;
    }

    public struct SwapChainDescription1
    {
        public int Width;
        public int Height;
        public Format Format;
        public bool Stereo;
        public SampleDescription SampleDescription;
        public Usage Usage;
        public int BufferCount;
        public SwapEffect SwapEffect;
        public Scaling Scaling;
        public SwapChainFlags Flags;
    }

    public sealed class PresentParameters
    {
    }

    public readonly struct PresentResult
    {
        public PresentResult(bool success)
        {
            Success = success;
        }

        public bool Success { get; }
    }

    public class SwapChain1 : IDisposable
    {
        public SwapChain1(SwapChainDescription1 description, IntPtr surfacePointer = default)
        {
            Description1 = description;
            Description = new SwapChainDescription
            {
                ModeDescription = new ModeDescription { Format = description.Format },
                Flags = description.Flags
            };
            SurfacePointer = surfacePointer;
        }

        public SwapChainDescription1 Description1 { get; private set; }

        public SwapChainDescription Description { get; private set; }

        public IntPtr SurfacePointer { get; }

        public bool IsDisposed { get; private set; }

        public virtual PresentResult Present(int syncInterval, PresentFlags presentFlags, PresentParameters presentParameters)
        {
            return new PresentResult(!IsDisposed);
        }

        public virtual void ResizeBuffers(int bufferCount, int width, int height, Format format, SwapChainFlags flags)
        {
            Description1 = new SwapChainDescription1
            {
                Width = width,
                Height = height,
                Format = format,
                Stereo = Description1.Stereo,
                SampleDescription = Description1.SampleDescription,
                Usage = Description1.Usage,
                BufferCount = bufferCount,
                SwapEffect = Description1.SwapEffect,
                Scaling = Description1.Scaling,
                Flags = flags
            };
            Description = new SwapChainDescription
            {
                ModeDescription = new ModeDescription { Format = format },
                Flags = flags
            };
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
        }
    }

    public sealed class SwapChain2 : SwapChain1
    {
        public SwapChain2(SwapChainDescription1 description)
            : base(description)
        {
        }
    }
}
