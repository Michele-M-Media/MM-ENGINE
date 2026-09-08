using System;
using System.Collections.Generic;
using SharpProspero.Interop.Sysmodule;
using SharpProspero.Modules;

namespace MMEngine.Platform.PS5;

/// <summary>Owns optional runtime modules and unloads them in reverse order.</summary>
public sealed class Ps5Modules : IDisposable
{
    private readonly List<SystemModule> _modules = [];
    private bool _disposed;

    public void LoadPng() => Load(SystemModuleId.PngDec);
    public void LoadTrueType()
    {
        Load(SystemModuleId.Font);
        Load(SystemModuleId.FontFt);
    }

    public void Load(SystemModuleId id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        for (int i = 0; i < _modules.Count; i++)
            if (_modules[i].Id == id)
                return;
        _modules.Add(SystemModule.Load(id));
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        for (int i = _modules.Count - 1; i >= 0; i--)
            _modules[i].Dispose();
        _modules.Clear();
    }
}
