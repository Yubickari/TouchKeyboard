#!/usr/bin/env bash
# Package smoke test: pack all four libraries, then consume them from temp
# projects through a local NuGet feed. This catches packaging mistakes that
# project references hide. The three consumer shapes match the release matrix:
# Avalonia only, Avalonia + Pinyin, Avalonia + Extended.
#
# The consumer uses the package's Avalonia compatibility floor (12.0.5) by
# default. Set AVALONIA_VERSION=12.1.5 to also exercise a newer consumer.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ -z "${NUGET_PACKAGES:-}" ]]; then
  # Keep restore inside the workspace so sandboxed and CI runs do not need
  # write access to the user profile.
  NUGET_PACKAGES="$ROOT/.nuget/packages"
fi
export NUGET_PACKAGES
export NUGET_HTTP_CACHE_PATH="${NUGET_HTTP_CACHE_PATH:-$ROOT/.nuget/http-cache}"

version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n 1)"
avalonia_version="${AVALONIA_VERSION:-12.0.5}"
feed="$ROOT/artifacts"
smoke_root="$ROOT/.tmp/smoke"

rm -rf "$feed" "$smoke_root"
mkdir -p "$smoke_root"

# A previous smoke run may have cached the same version in the global package
# folder. Remove only the TouchKeyboard packages so the local feed is used.
for package in touchkeyboard.core touchkeyboard.avalonia \
               touchkeyboard.ime.pinyin touchkeyboard.ime.pinyin.extended; do
  rm -rf "$NUGET_PACKAGES/$package"
done

echo "smoke: pack (Avalonia $avalonia_version)"
dotnet pack src/TouchKeyboard.Core/TouchKeyboard.Core.csproj -c Release -o "$feed"
dotnet pack src/TouchKeyboard.Avalonia/TouchKeyboard.Avalonia.csproj -c Release -o "$feed"
dotnet pack src/TouchKeyboard.Ime.Pinyin/TouchKeyboard.Ime.Pinyin.csproj -c Release -o "$feed"
dotnet pack src/TouchKeyboard.Ime.Pinyin.Extended/TouchKeyboard.Ime.Pinyin.Extended.csproj -c Release -o "$feed"

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

echo "smoke: Avalonia package only (no language pack dependency)"
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
    <PackageReference Include="Avalonia" Version="$avalonia_version" />
    <PackageReference Include="Avalonia.Desktop" Version="$avalonia_version" />
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
        var options = new TouchKeyboard.Avalonia.TouchKeyboardOptions();
        if (options.Ime.Engine is not null)
        {
            throw new InvalidOperationException("Avalonia-only smoke unexpectedly has an IME engine.");
        }

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

if grep -q 'TouchKeyboard.Ime.Pinyin' "$avalonia_dir/obj/project.assets.json"; then
  echo "smoke: Avalonia-only project unexpectedly depends on a language pack" >&2
  exit 1
fi

write_ime_smoke() {
  local dir="$1"
  local package="$2"
  local engineNamespace="$3"
  local engineType="$4"
  local input="$5"
  local expected="$6"

  mkdir -p "$dir"
  write_nuget_config "$dir"
  write_local_props "$dir"
  cat > "$dir/PinyinSmoke.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TouchKeyboard.Avalonia" Version="$version" />
    <PackageReference Include="$package" Version="$version" />
  </ItemGroup>
</Project>
EOF
  cat > "$dir/Program.cs" <<EOF
using TouchKeyboard.Core;
using $engineNamespace;

var engine = new $engineType();
if (!engine.TryQuery("$input", 5, out var candidates)
    || candidates.Count == 0
    || candidates[0].Text != "$expected")
{
    Console.Error.WriteLine("unexpected candidates: " + string.Join(" | ", candidates.Select(c => c.Text)));
    return 1;
}

var options = new TouchKeyboard.Avalonia.TouchKeyboardOptions();
options.UseIme(engine);
if (options.Ime.Engine is null)
{
    Console.Error.WriteLine("UseIme did not store the engine.");
    return 1;
}

Console.WriteLine("IME smoke ok: $input -> " + candidates[0].Text);
return 0;
EOF
  dotnet run --project "$dir/PinyinSmoke.csproj" -c Release
}

echo "smoke: Avalonia + Pinyin package"
write_ime_smoke "$smoke_root/pinyin" "TouchKeyboard.Ime.Pinyin" "TouchKeyboard.Ime.Pinyin" "PinyinImeEngine" "nihao" "你好"

echo "smoke: Avalonia + Extended package"
write_ime_smoke "$smoke_root/extended" "TouchKeyboard.Ime.Pinyin.Extended" "TouchKeyboard.Ime.Pinyin.Extended" "ExtendedPinyinImeEngine" "houtougu" "猴头菇"

echo "smoke: AOT/trim publish"
aot_dir="$smoke_root/aot"
mkdir -p "$aot_dir"
write_nuget_config "$aot_dir"
write_local_props "$aot_dir"
cat > "$aot_dir/AotSmoke.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TouchKeyboard.Core" Version="$version" />
    <PackageReference Include="TouchKeyboard.Ime.Pinyin" Version="$version" />
    <PackageReference Include="TouchKeyboard.Ime.Pinyin.Extended" Version="$version" />
  </ItemGroup>
</Project>
EOF
cat > "$aot_dir/Program.cs" <<'EOF'
using TouchKeyboard.Core;
using TouchKeyboard.Ime.Pinyin;
using TouchKeyboard.Ime.Pinyin.Extended;

var common = new PinyinImeEngine();
var extended = new ExtendedPinyinImeEngine();
var failures = 0;

if (!common.TryQuery("nihao", 5, out var commonCandidates)
    || commonCandidates[0].Text != "你好")
{
    Console.Error.WriteLine("common dictionary query failed");
    failures++;
}

if (!extended.TryQuery("houtougu", 5, out var extendedCandidates)
    || !extendedCandidates.Select(c => c.Text).Contains("猴头菇"))
{
    Console.Error.WriteLine("extended dictionary query failed");
    failures++;
}

var session = new KeyboardSession(new KeyboardSessionOptions
{
    Profile = InputProfiles.Text,
    ImeEngine = common
});
foreach (var letter in "nihao")
{
    session.Execute(KeyCommand.Insert(letter.ToString()));
}
if (!session.TryCommit(out var text, out _) || text != "你好")
{
    Console.Error.WriteLine("session composition failed");
    failures++;
}

return failures;
EOF
dotnet publish "$aot_dir/AotSmoke.csproj" -c Release -p:PublishAot=true -r "$(dotnet --info | awk '/RID:/ { print $2; exit }')" -o "$aot_dir/publish"
"$aot_dir/publish/AotSmoke" && echo "smoke: AOT publish ran successfully"

echo "smoke: ok ($version)"
