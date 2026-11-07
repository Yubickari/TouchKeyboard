using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// Exposes a touch control as a button so its accessible name reaches the
/// automation tree; a plain <see cref="Border"/> would be reported as a
/// non-control element.
/// </summary>
internal sealed class KeyboardAutomationPeer : ControlAutomationPeer
{
    private readonly string _className;

    public KeyboardAutomationPeer(Control owner, string className)
        : base(owner)
    {
        _className = className;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

    protected override string GetClassNameCore() => _className;
}
