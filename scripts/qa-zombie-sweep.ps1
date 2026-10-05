# qa-zombie-sweep.ps1 - kill headless-browser leftovers from QA passes, and ONLY those.
# Matches on COMMAND LINE, never on bare image name: the owner's real Chrome/Brave and the
# dev server must survive (a 2026-09-29 `taskkill /im testhost.exe` killed other lanes' runs).
#   chrome.exe  whose command line matches  ms-playwright | playwright_chro | .codegpt\ab-sessions
#   node.exe    whose command line matches  browser.mjs
# Always exits 0: a sweep that finds nothing is a success.
$rules = @(
    @{ Name = 'chrome.exe'; Pattern = 'ms-playwright|playwright_chro|\.codegpt\ab-sessions' },
    @{ Name = 'node.exe';   Pattern = 'browser\.mjs' }
)
$killed = 0
foreach ($rule in $rules) {
    $procs = @()
    try { $procs = @(Get-CimInstance Win32_Process -Filter ("Name = '" + $rule.Name + "'")) } catch { $procs = @() }
    foreach ($p in $procs) {
        $cmd = [string]$p.CommandLine
        if ($cmd -and $p.ProcessId -ne $PID -and ($cmd -match $rule.Pattern)) {
            $show = $cmd
            if ($show.Length -gt 80) { $show = $show.Substring(0, 80) }
            try {
                Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop
                Wait-Process -Id $p.ProcessId -Timeout 3 -ErrorAction SilentlyContinue   # let it actually die before the next sweep
                Write-Output ("killed {0} {1} :: {2}" -f $p.ProcessId, $p.Name, $show)
                $killed++
            } catch {
                # already gone (child of a tree we just killed) - fine
            }
        }
    }
}
if ($killed -eq 0) { Write-Output 'nothing to sweep' } else { Write-Output ("swept {0} process(es)" -f $killed) }
exit 0
