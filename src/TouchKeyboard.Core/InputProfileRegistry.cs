using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TouchKeyboard.Core;

/// <summary>
/// Case-insensitive registry of input profiles. It resolves a profile id from
/// XAML or configuration without exposing a closed enum.
/// </summary>
public static class InputProfileRegistry
{
    private static readonly ConcurrentDictionary<string, InputProfile> Profiles =
        new ConcurrentDictionary<string, InputProfile>(StringComparer.OrdinalIgnoreCase);

    static InputProfileRegistry()
    {
        foreach (var profile in InputProfiles.All)
        {
            Profiles.TryAdd(profile.Id, profile);
        }
    }

    /// <summary>Finds a profile by id.</summary>
    public static bool TryGet(string? id, out InputProfile? profile)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            profile = null;
            return false;
        }

        if (Profiles.TryGetValue(id!, out var found))
        {
            profile = found;
            return true;
        }

        profile = null;
        return false;
    }

    /// <summary>Finds a profile by id or throws.</summary>
    public static InputProfile Get(string id)
    {
        if (!TryGet(id, out var profile) || profile is null)
        {
            throw new KeyNotFoundException($"Unknown input profile '{id}'.");
        }

        return profile;
    }

    /// <summary>Registers a custom profile. Built-in ids cannot be replaced.</summary>
    public static void Register(InputProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (!Profiles.TryAdd(profile.Id, profile))
        {
            throw new InvalidOperationException($"An input profile with id '{profile.Id}' is already registered.");
        }
    }
}
