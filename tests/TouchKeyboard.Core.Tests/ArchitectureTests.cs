using System.Linq;
using TouchKeyboard.Core;
using Xunit;

namespace TouchKeyboard.Core.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void Core_does_not_reference_any_ui_or_platform_assembly()
    {
        var references = typeof(KeyboardSession).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(references, name => name.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, name => name.Contains("PresentationFramework", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, name => name.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, name => name.Contains("X11", StringComparison.OrdinalIgnoreCase));
    }
}
