<#
The inner loop, in seconds instead of minutes (#251 item 4).

  ./test-fast.ps1                 -> the FAST run: everything except the [SlowGate] classes
  ./test-fast.ps1 -Full           -> the FULL run, exactly what CI runs. Nothing is filtered.
  ./test-fast.ps1 -Slow           -> ONLY the slow gates, for when you touched one of them
  ./test-fast.ps1 -Core           -> fast run, Core suite only
  ./test-fast.ps1 -Client         -> fast run, Client suite only
  ./test-fast.ps1 -Filter "X"     -> AND your own --filter expression onto whichever run you chose
  ./test-fast.ps1 -Trx            -> also write .trx files, so you can re-measure the class totals

Everything here is a wrapper around one `dotnet test --filter` invocation, printed before it runs so
you can copy it. There is no build system magic and no second project: the tags are xUnit traits and
the filter is `speed!=slow`.

WHAT THE FAST RUN DOES NOT TELL YOU. It skips 64 test classes — 733 tests, 12.7% of the suite — that
between them hold 93% of the suite's measured seconds: the N-body and long-flight gates, the traffic
and surface generators, the A* walkability audits, the boot sweeps and the snapshot fingerprints. A
green fast run means the RULES still hold. It does not mean the ship still flies, the floors are
still walkable, or the boot still builds the world it always built. Before you push, run it full.

CI ALWAYS RUNS THE FULL SUITE (.github/workflows/ci.yml is untouched by this). The fast run is a
convenience for the person typing; the merge gate is the whole contract, as it always was.

The roster of what is skipped, with the seconds each class cost, is checked in and guarded:
tests/SpaceSails.Core.Tests/TheSlowGateRosterTests.cs (21 classes) and its Client twin (43). See
docs/testing-guide.md, Appendix C.
#>
param(
    [switch]$Full,
    [switch]$Slow,
    [switch]$Core,
    [switch]$Client,
    [string]$Filter = "",
    [switch]$Trx,
    [switch]$NoBuild
)

if ($Full -and $Slow) {
    Write-Host "  -Full and -Slow ask for opposite things. Pick one." -ForegroundColor Red
    exit 1
}
if ($Core -and $Client) {
    Write-Host "  -Core and -Client ask for opposite things. Pick one, or neither for both." -ForegroundColor Red
    exit 1
}

$target =
    if ($Core) { "tests/SpaceSails.Core.Tests" }
    elseif ($Client) { "tests/SpaceSails.Client.Tests" }
    else { "SpaceSails.slnx" }

$speed =
    if ($Full) { "" }
    elseif ($Slow) { "speed=slow" }
    else { "speed!=slow" }

$expr = @($speed, $Filter) | Where-Object { $_ } | ForEach-Object { "($_)" }
$expr = $expr -join "&"

$argv = @("test", $target, "-c", "Release")
if ($NoBuild) { $argv += "--no-build" }
if ($expr) { $argv += @("--filter", $expr) }
if ($Trx) { $argv += @("--logger", "trx", "--results-directory", "TestResults") }

$label =
    if ($Full) { "FULL - the whole contract, same as CI" }
    elseif ($Slow) { "SLOW GATES ONLY - the 64 classes on the roster" }
    else { "FAST - skipping the 64 [SlowGate] classes (733 tests, 12.7% of the suite)" }

Write-Host ""
Write-Host "  $label" -ForegroundColor Cyan
Write-Host "  dotnet $($argv -join ' ')" -ForegroundColor DarkGray
if (-not $Full -and -not $Slow) {
    Write-Host "  A green fast run means the RULES hold. It does not mean the ship flies." -ForegroundColor Yellow
    Write-Host "  Run ./test-fast.ps1 -Full before you push." -ForegroundColor Yellow
}
Write-Host ""

& dotnet @argv
exit $LASTEXITCODE
