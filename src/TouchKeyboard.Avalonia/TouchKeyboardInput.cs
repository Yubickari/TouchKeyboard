using Avalonia;
using Avalonia.Controls;

namespace TouchKeyboard.Avalonia;

/// <summary>
/// UI metadata attached to an input control: the input profile, header label,
/// unit and the per-control enable switch.
/// </summary>
public static class TouchKeyboardInput
{
    /// <summary>
    /// Input profile id, for example <c>positive-decimal</c> or <c>date-dot</c>.
    /// Unknown ids fall back to the control-type default in the controller.
    /// </summary>
    public static readonly AttachedProperty<string?> ProfileProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>(
            "Profile",
            typeof(TouchKeyboardInput));

    /// <summary>
    /// Field description shown in the keyboard title row.
    /// </summary>
    public static readonly AttachedProperty<string?> LabelProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>(
            "Label",
            typeof(TouchKeyboardInput));

    /// <summary>
    /// Unit shown next to the field description, for example "℃".
    /// </summary>
    public static readonly AttachedProperty<string?> UnitProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>(
            "Unit",
            typeof(TouchKeyboardInput));

    /// <summary>
    /// Per-control switch. When false, the controller must not open the keyboard
    /// for this control or its descendants.
    /// </summary>
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "Enabled",
            typeof(TouchKeyboardInput),
            defaultValue: true);

    /// <summary>Gets the input profile id.</summary>
    public static string? GetProfile(Control control) => control.GetValue(ProfileProperty);

    /// <summary>Sets the input profile id.</summary>
    public static void SetProfile(Control control, string? value) =>
        control.SetValue(ProfileProperty, value);

    /// <summary>Gets the field description.</summary>
    public static string? GetLabel(Control control) => control.GetValue(LabelProperty);

    /// <summary>Sets the field description.</summary>
    public static void SetLabel(Control control, string? value) =>
        control.SetValue(LabelProperty, value);

    /// <summary>Gets the unit.</summary>
    public static string? GetUnit(Control control) => control.GetValue(UnitProperty);

    /// <summary>Sets the unit.</summary>
    public static void SetUnit(Control control, string? value) =>
        control.SetValue(UnitProperty, value);

    /// <summary>Gets whether the keyboard is enabled for the control.</summary>
    public static bool GetEnabled(Control control) => control.GetValue(EnabledProperty);

    /// <summary>Sets whether the keyboard is enabled for the control.</summary>
    public static void SetEnabled(Control control, bool value) =>
        control.SetValue(EnabledProperty, value);
}
