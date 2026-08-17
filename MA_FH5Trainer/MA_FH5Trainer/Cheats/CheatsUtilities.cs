using System.Windows;
using System.Collections.Concurrent;
using HorizonTuner.Models;
using MahApps.Metro.Controls;
using Memory;
using static HorizonTuner.Resources.Memory;

namespace HorizonTuner.Cheats;

public class CheatsUtilities
{
    private static readonly ConcurrentDictionary<string, AoBScanDiagnostics?> s_lastAobDiagnostics = new();

    public static bool TryGetLastAobDiagnostics(string signature, out AoBScanDiagnostics? diagnostics)
    {
        if (!s_lastAobDiagnostics.TryGetValue(signature, out diagnostics))
        {
            diagnostics = null;
            return false;
        }

        return diagnostics != null;
    }

    protected static async Task<nuint> SmartAobScan(string search, UIntPtr? start = null, UIntPtr? end = null)
    {
        var minRange = (long)GetInstance().MProc.Process.MainModule!.BaseAddress;
        var maxRange = minRange + GetInstance().MProc.Process.MainModule!.ModuleMemorySize;

        if (start != null)
        {
            minRange = (long)start;
        }
        
        if (end != null)
        {
            maxRange = (long)end;
        }
        
        var result = await GetInstance().AoBScanWithDiagnostics(minRange, maxRange, search, writable: false, executable: true, mapped: false, resultLimit: 1);
        s_lastAobDiagnostics.AddOrUpdate(search, result.Diagnostics, (_, _) => result.Diagnostics);
        return result.Addresses.Count > 0 ? result.Addresses[0] : 0;
    }
    
    protected static void ShowError(string feature, string sig)
    {
        string diagText = string.Empty;
        if (TryGetLastAobDiagnostics(sig, out var diag) && diag != null)
        {
            var readsTotal = diag.ReadsOk + diag.ReadsFailed + diag.ReadsPartial;
            var failRate = readsTotal > 0 ? (double)diag.ReadsFailed / readsTotal : 0d;
            diagText =
                $"\n\nAoB Diagnostics:\nRange: 0x{diag.RangeStart:X}-0x{diag.RangeEnd:X}\nRegions: {diag.RegionsVisited} (eligible={diag.RegionsEligible})\nBytes: {diag.EligibleBytes}\nReads: ok={diag.ReadsOk} fail={diag.ReadsFailed} partial={diag.ReadsPartial} allocFail={diag.AllocFailed}\nFailRate: {failRate:P1}\nMatches: {diag.Matches}\nElapsedMs: {diag.ElapsedMs:0.0}\nLastError: {diag.LastError}";
        }

        MessageBox.Show(
            $"Address for this feature wasn't found!\nPlease try to activate the cheat again or try to restart the game and the tool.\n\nIf this error still occurs, please (Press Ctrl+C) to copy, and make an issue on the GitHub repository or post the copied text in the in our discord server (discord.gg/rHzev9brJ3).\n\nFeature: {feature}\nSignature: {sig}{diagText}\n\nTool Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}\nGame: {GameVerPlat.GetInstance().Name}\nGame Version: {GameVerPlat.GetInstance().Update}\nPlatform: {GameVerPlat.GetInstance().Platform}",
            $"HorizonTuner - Error", 0, MessageBoxImage.Error);
    }

    protected static void Free(UIntPtr address)
    {
        if (address == 0) return;
        var handle = GetInstance().MProc.Handle;
        Imps.VirtualFreeEx(handle, address,0, Imps.MemRelease);
    }
    
    protected static byte[] CalculateDetour(nuint address, nuint target, int replaceCount)
    {
        var detourBytes = new byte[replaceCount];
        detourBytes[0] = 0xE9;
        BitConverter.GetBytes((int)((long)target - (long)address - 5)).CopyTo(detourBytes, 1);
        
        for (var i = 5; i < detourBytes.Length; i++)
        {
            detourBytes[i] = 0x90;
        }

        return detourBytes;
    }
}
