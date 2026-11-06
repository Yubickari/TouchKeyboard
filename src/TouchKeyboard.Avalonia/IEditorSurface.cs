using Avalonia.Controls;
using TouchKeyboard.Core;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// The value display/editing surface above the keys. The default implementation
/// is <see cref="TouchKeyboardEditor"/>; a host may inject its own control to fit
/// an existing frontend architecture.
/// </summary>
public interface IEditorSurface
{
    /// <summary>The control to place in the host's editor row.</summary>
    Control View { get; }

    /// <summary>Binds the surface to a session and starts rendering it.</summary>
    void Attach(KeyboardSession session);

    /// <summary>Unbinds the surface; called when the panel closes.</summary>
    void Detach();
}
