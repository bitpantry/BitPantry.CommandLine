using System.Runtime.CompilerServices;
using VerifyTests;

namespace BitPantry.CommandLine.Tests.Snapshots;

/// <summary>
/// Module initializer for Verify snapshot testing.
/// Uses default settings which stores .verified.txt files next to the test source file.
/// </summary>
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Disable unique prefix validation - needed when running all tests together
        // as Verify's caching can cause false positives for "duplicate prefix" detection
        VerifierSettings.DisableRequireUniquePrefix();
    }
}
