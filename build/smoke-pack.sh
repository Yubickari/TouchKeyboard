#!/usr/bin/env bash
# Package smoke test: pack both libraries, then consume them from temp projects
# through a local NuGet feed. This catches packaging mistakes that project
# references hide.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ -z "${NUGET_PACKAGES:-}" ]]; then
  NUGET_PACKAGES="${HOME:-$ROOT}/.nuget/packages"
fi
export NUGET_PACKAGES
export NUGET_HTTP_CACHE_PATH="${NUGET_HTTP_CACHE_PATH:-$ROOT/.nuget/http-cache}"

version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)"
feed="$ROOT/artifacts"
smoke_root="$ROOT/.tmp/smoke"

rm -rf "$feed" "$smoke_root"
mkdir -p "$smoke_root"

echo "smoke: pack"
dotnet pack src/TouchKeyboard.Core/TouchKeyboard.Core.csproj -c Release -o "$feed"
dotnet pack src/TouchKeyboard.Avalonia/TouchKeyboard.Avalonia.csproj -c Release -o "$feed"

write_nuget_config() {
  local dir="$1"
  cat > "$dir/NuGet.Config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="$NUGET_PACKAGES" />
  </config>
  <packageSources>
    <clear />
    <add key="local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF
}

# The smoke projects live inside this repository, so they would inherit central
# package management. A local Directory.Packages.props disables it and keeps the
# package references versioned and self-contained.
write_local_props() {
  local dir="$1"
  cat > "$dir/Directory.Packages.props" <<'EOF'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
EOF
}

echo "smoke: Core package from a plain console project"
core_dir="$smoke_root/core"
mkdir -p "$core_dir"
write_nuget_config "$core_dir"
write_local_props "$core_dir"
cat > "$core_dir/core.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TouchKeyboard.Core" Version="$version" />
  </ItemGroup>
</Project>
EOF
cat > "$core_dir/Program.cs" <<'EOF'
Console.WriteLine(TouchKeyboard.Core.KeyboardMode.FullKeyboard);
EOF
dotnet build "$core_dir" -c Release

echo "smoke: Avalonia package from a minimal Avalonia app"
avalonia_dir="$smoke_root/avalonia"
mkdir -p "$avalonia_dir"
write_nuget_config "$avalonia_dir"
write_local_props "$avalonia_dir"
cat > "$avalonia_dir/AvaloniaSmoke.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="12.0.5" />
    <PackageReference Include="Avalonia.Desktop" Version="12.0.5" />
    <PackageReference Include="TouchKeyboard.Avalonia" Version="$version" />
  </ItemGroup>
</Project>
EOF
cat > "$avalonia_dir/Program.cs" <<'EOF'
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace SmokeAvalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        _ = typeof(TouchKeyboard.Avalonia.TouchKeyboardHost);
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}

public sealed class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Window
            {
                Title = "TouchKeyboard package smoke",
                Content = new TextBlock { Text = "TouchKeyboard.Avalonia restored and loaded." }
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
EOF
dotnet build "$avalonia_dir/AvaloniaSmoke.csproj" -c Release

echo "smoke: ok ($version)"
