"""Static checks (rubric A1, F1, F2): does it build, do tests exist, do they pass.

These shell out to the .NET SDK. They inspect only outcomes (exit codes, test counts) — never
*how* the code is written — so they stay framework-neutral.
"""

from __future__ import annotations

import os
import re
import subprocess
from pathlib import Path


def _run(cmd: list[str], cwd: Path, timeout: int = 1200) -> tuple[int, str]:
    proc = subprocess.run(
        cmd, cwd=str(cwd), capture_output=True, text=True, timeout=timeout,
        env={**os.environ, "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1"},
    )
    return proc.returncode, (proc.stdout or "") + "\n" + (proc.stderr or "")


def find_build_target(run_dir: Path) -> Path | None:
    """Prefer a solution file at/near the root; fall back to letting `dotnet` discover."""
    for pat in ("*.slnx", "*.sln"):
        hits = sorted(run_dir.glob(pat)) + sorted(run_dir.glob(f"**/{pat}"))
        if hits:
            return hits[0]
    return None


def find_test_projects(run_dir: Path) -> list[Path]:
    tests = []
    for csproj in run_dir.glob("**/*.csproj"):
        if any(p in ("bin", "obj") for p in csproj.parts):
            continue
        text = csproj.read_text(encoding="utf-8", errors="ignore").lower()
        if ("microsoft.net.test.sdk" in text or "xunit" in text or "nunit" in text
                or "mstest" in text or "microsoft.testing.platform" in text
                or csproj.stem.lower().endswith("tests") or csproj.stem.lower().endswith("test")):
            tests.append(csproj)
    return tests


def build(run_dir: Path) -> dict:
    target = find_build_target(run_dir)
    # NuGetAudit is disabled: it fails the build on *transitive* package CVEs that are
    # published over time and are unrelated to the generated code's quality, which would make
    # the benchmark irreproducible. Real compiler errors/warnings still fail the build.
    cmd = ["dotnet", "build", "-c", "Release", "-p:NuGetAudit=false"]
    if target:
        cmd.insert(2, str(target))
    code, log = _run(cmd, run_dir)
    return {"ok": code == 0, "target": str(target) if target else "(discovered)", "log_tail": _tail(log)}


def test(run_dir: Path) -> dict:
    projects = find_test_projects(run_dir)
    if not projects:
        return {"exists": False, "passed": False, "evidence": "no test project found"}
    target = find_build_target(run_dir)
    # No --logger/--nologo: the Trellis template uses xUnit v3 on Microsoft.Testing.Platform,
    # which rejects those VSTest flags (exit code 5, "Zero tests ran").
    cmd = ["dotnet", "test", "-c", "Release", "-p:NuGetAudit=false"]
    if target:
        cmd.insert(2, str(target))
    code, log = _run(cmd, run_dir)
    total, failed = _parse_test_counts(log)
    ran = total > 0
    # Decide on the test counts, not the raw exit code: Microsoft.Testing.Platform returns a
    # non-zero exit when an *empty* test project reports "Zero tests ran", which should not by
    # itself fail a suite whose real tests all pass.
    passed = ran and failed == 0
    return {
        "exists": True,
        "ran": ran,
        "passed": passed,
        "projects": [str(p.relative_to(run_dir)) for p in projects],
        "passed_total": total - failed,
        "failed_total": failed,
        "exit_code": code,
        "evidence": _tail(log),
    }


def _parse_test_counts(log: str) -> tuple[int, int]:
    """Return (total, failed), tolerant of both Microsoft.Testing.Platform and VSTest output."""
    import re
    # MTP summary (lowercase): "  total: 75" / "  failed: 1"
    mt = re.findall(r"^\s*total:\s+(\d+)\s*$", log, re.M | re.I)
    if mt:
        mf = re.findall(r"^\s*failed:\s+(\d+)\s*$", log, re.M | re.I)
        return sum(int(x) for x in mt), (sum(int(x) for x in mf) if mf else 0)
    # VSTest summary: "Failed: 0, Passed: 5, Skipped: 0, Total: 5"
    vp = [int(x) for x in re.findall(r"Passed:\s+(\d+)", log)]
    vf = [int(x) for x in re.findall(r"Failed:\s+(\d+)", log)]
    vs = [int(x) for x in re.findall(r"Skipped:\s+(\d+)", log)]
    if vp or vf:
        return sum(vp) + sum(vf) + sum(vs), sum(vf)
    if re.search(r"Passed!", log) and not re.search(r"Failed!", log):
        return 1, 0
    return 0, 0


def _tail(log: str, n: int = 40) -> str:
    lines = [ln for ln in log.splitlines() if ln.strip()]
    return "\n".join(lines[-n:])
