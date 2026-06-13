"""Run the full neutral evaluation for one generated service (or all of them).

Pipeline per run directory:
  1. build            -> A1
  2. locate + boot the API (single `dotnet <dll>` process, http, Development)
  3. black-box probe  -> A2, B*, C*, D*, E*
  4. run the tests    -> F1, F2
  5. write result.json (all 30 criteria, 1/0, with evidence)

Usage:
  python run_all.py runs/with-trellis/opus-4.8/run-1
  python run_all.py --all
"""

from __future__ import annotations

import argparse
import json
import os
import socket
import subprocess
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

import requests

import static_checks
from criteria import ALL_IDS, CRITERIA
from probe import run_probe, find_leaks

HERE = Path(__file__).resolve().parent
REPO = HERE.parent


def free_port() -> int:
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.bind(("127.0.0.1", 0))
    port = s.getsockname()[1]
    s.close()
    return port


def find_api_project(run_dir: Path) -> tuple[Path, Path] | None:
    """Return (project_dir, dll_path) for the web host, or None if not found."""
    candidates = []
    for prog in run_dir.glob("**/Program.cs"):
        if any(p in ("bin", "obj") for p in prog.parts):
            continue
        text = prog.read_text(encoding="utf-8", errors="ignore")
        if "WebApplication" in text or "CreateSlimBuilder" in text or "CreateBuilder" in text:
            csproj = _nearest_csproj(prog)
            if csproj:
                candidates.append(csproj)
    # Prefer a project that is NOT a test project and looks like the host (Sdk.Web).
    for csproj in candidates:
        t = csproj.read_text(encoding="utf-8", errors="ignore").lower()
        if "sdk.web" in t or ("microsoft.net.test.sdk" not in t and "xunit" not in t):
            dll = _find_dll(csproj)
            if dll:
                return csproj.parent, dll
    for csproj in candidates:
        dll = _find_dll(csproj)
        if dll:
            return csproj.parent, dll
    return None


def _nearest_csproj(start: Path) -> Path | None:
    d = start.parent
    while d and str(d).startswith(str(d.anchor)):
        hits = list(d.glob("*.csproj"))
        if hits:
            return hits[0]
        if d == d.parent:
            break
        d = d.parent
    return None


def _find_dll(csproj: Path) -> Path | None:
    """Locate the bootable app DLL by its sibling <name>.runtimeconfig.json (which only the
    runnable entry-point assembly has). Robust to AssemblyName differing from the csproj name."""
    out = csproj.parent / "bin" / "Release"
    if not out.exists():
        return None
    cfgs = list(out.glob("**/*.runtimeconfig.json"))
    cfgs.sort(key=lambda p: ("net10" not in str(p), str(p)))  # prefer net10.0
    for cfg in cfgs:
        dll = cfg.with_name(cfg.name[: -len(".runtimeconfig.json")] + ".dll")
        if dll.exists():
            return dll
    return None


def boot_and_probe(run_dir: Path, project_dir: Path, dll: Path) -> dict:
    port = free_port()
    base = f"http://127.0.0.1:{port}"
    for db in project_dir.glob("*.db"):
        try:
            db.unlink()
        except OSError:
            pass
    env = {
        **os.environ,
        "ASPNETCORE_URLS": base,
        "ASPNETCORE_ENVIRONMENT": "Development",
        "DOTNET_ENVIRONMENT": "Development",
        "DOTNET_NOLOGO": "1",
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
    }
    proc = subprocess.Popen(
        ["dotnet", str(dll)], cwd=str(project_dir), env=env,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
    )
    try:
        if not _wait_health(base, proc, timeout=75):
            return {"booted": False, "base_url": base, "criteria": {}}
        out = run_probe(base)
        out["booted"] = True
        out["base_url"] = base
        return out
    finally:
        _kill(proc)


def _wait_health(base: str, proc: subprocess.Popen, timeout: int) -> bool:
    deadline = time.time() + timeout
    while time.time() < deadline:
        if proc.poll() is not None:
            return False  # process exited early
        try:
            r = requests.get(base + "/health", timeout=3)
            if r.status_code < 500:
                return True
        except requests.RequestException:
            time.sleep(0.5)
    return False


def _kill(proc: subprocess.Popen):
    if proc.poll() is None:
        try:
            proc.terminate()
            proc.wait(timeout=10)
        except Exception:
            try:
                proc.kill()
            except Exception:
                pass


def production_leak_check(project_dir: Path, dll: Path, api_version: str | None):
    """Score E4 fairly: boot the service in PRODUCTION and fire malformed-input requests
    (which throw in the pipeline, before any DB access), asserting no response body leaks a
    stack trace, exception type, or source path. Development is unsuitable because ASP.NET's
    developer exception page leaks by framework default — that is not a production
    vulnerability. Returns (ok: bool | None, evidence); None => could not boot in Production,
    so E4 is not applicable for this run."""
    ver = api_version or "2026-11-12"
    port = free_port()
    base = f"http://127.0.0.1:{port}"
    for db in project_dir.glob("*.db"):
        try:
            db.unlink()
        except OSError:
            pass
    env = {
        **os.environ,
        "ASPNETCORE_URLS": base,
        "ASPNETCORE_ENVIRONMENT": "Production",
        "DOTNET_ENVIRONMENT": "Production",
        "DOTNET_NOLOGO": "1",
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
    }
    proc = subprocess.Popen(
        ["dotnet", str(dll)], cwd=str(project_dir), env=env,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
    )
    try:
        if not _wait_health(base, proc, 60):
            return None, "service did not boot in Production; E4 not applicable"
        v = f"?api-version={ver}"
        good = ('{"firstName":"A","lastName":"B","email":"leak@probe.test",'
                '"shippingAddress":{"street":"s","city":"c","state":"x",'
                '"postalCode":"p","country":"co"}}')
        attempts = [
            # valid body + valid version + MALFORMED actor header -> actor parse throws
            ("POST", "/api/customers", {"X-Test-Actor": "not-json-{{{",
                                        "Content-Type": "application/json"}, good),
            ("POST", "/api/customers", {"X-Test-Actor": '{"id":"x","permissions":"orders:create"}',
                                        "Content-Type": "application/json"}, good),
            # malformed / wrong-typed JSON body -> model binding throws
            ("POST", "/api/customers", {"Content-Type": "application/json"}, '{"firstName":'),
            ("POST", "/api/products", {"Content-Type": "application/json"},
                                       '{"productName":123,"sku":true,"unitPrice":"abc"}'),
        ]
        bodies = []
        for method, path, headers, data in attempts:
            try:
                r = requests.request(method, base + path + v, headers=headers, data=data, timeout=20)
                bodies.append(r.text or "")
            except requests.RequestException:
                pass
        leaks = find_leaks(bodies)
        return (not leaks), ("no internal leakage in production error responses"
                             if not leaks else f"production response leaked: {leaks}")
    finally:
        _kill(proc)


def evaluate(run_dir: Path, do_test: bool = True) -> dict:
    run_dir = run_dir.resolve()
    meta = _read_meta(run_dir)
    result = {cid: {"pass": 0, "evidence": "not evaluated"} for cid in ALL_IDS}

    # 1. build (A1)
    b = static_checks.build(run_dir)
    result["A1"] = {"pass": 1 if b["ok"] else 0, "evidence": b["log_tail"][-600:]}

    api_version = None
    if b["ok"]:
        # 2-3. boot + probe
        api = find_api_project(run_dir)
        if not api:
            result["A2"]["evidence"] = "no web host project found"
        else:
            project_dir, dll = api
            probe_out = boot_and_probe(run_dir, project_dir, dll)
            api_version = probe_out.get("api_version")
            if not probe_out.get("booted"):
                result["A2"] = {"pass": 0, "evidence": "service failed to start within timeout"}
            else:
                for cid, val in probe_out.get("criteria", {}).items():
                    result[cid] = val
                # E4 (no internal leak) — scored against a PRODUCTION boot, since the dev
                # developer-exception page leaks by framework default (not a prod vuln).
                leak_ok, leak_ev = production_leak_check(project_dir, dll, api_version)
                result["E4"] = {"pass": (None if leak_ok is None else (1 if leak_ok else 0)),
                                "evidence": leak_ev}
    else:
        result["A2"]["evidence"] = "build failed"

    # 4. tests (F1/F2)
    if do_test:
        t = static_checks.test(run_dir)
        result["F1"] = {"pass": 1 if t.get("exists") and t.get("ran") else 0,
                        "evidence": f"projects={t.get('projects')} ran={t.get('ran')}"}
        result["F2"] = {"pass": 1 if t.get("passed") else 0,
                        "evidence": f"passed={t.get('passed_total')} failed={t.get('failed_total')}"}

    numeric = [v["pass"] for v in result.values() if v.get("pass") is not None]
    passed = sum(numeric)
    total = len(numeric)
    out = {
        **meta,
        "evaluated_at": datetime.now(timezone.utc).isoformat(),
        "api_version": api_version,
        "score": {"passed": passed, "total": total,
                  "rate": round(passed / total, 3) if total else 0.0},
        "criteria": result,
    }
    (run_dir / "result.json").write_text(json.dumps(out, indent=2), encoding="utf-8")
    return out


def _read_meta(run_dir: Path) -> dict:
    mp = run_dir / "meta.json"
    if mp.exists():
        try:
            m = json.loads(mp.read_text(encoding="utf-8"))
            return {k: m.get(k) for k in ("condition", "model", "run") if k in m}
        except Exception:
            pass
    parts = run_dir.parts
    out = {}
    for i, p in enumerate(parts):
        if p in ("with-trellis", "without-trellis"):
            out["condition"] = p
            if i + 1 < len(parts):
                out["model"] = parts[i + 1]
            if i + 2 < len(parts):
                out["run"] = parts[i + 2]
    return out


def _is_run_dir(d: Path) -> bool:
    return any(d.glob("**/*.csproj"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("run_dir", nargs="?", help="a single run directory")
    ap.add_argument("--all", action="store_true", help="evaluate every run dir under runs/")
    ap.add_argument("--no-test", action="store_true")
    args = ap.parse_args()

    targets: list[Path] = []
    if args.all:
        runs_root = REPO / "runs"
        for cond in ("with-trellis", "without-trellis"):
            for d in sorted((runs_root / cond).glob("*/*")):
                if d.is_dir() and _is_run_dir(d):
                    targets.append(d)
    elif args.run_dir:
        targets = [Path(args.run_dir)]
    else:
        ap.error("provide a run_dir or --all")

    if not targets:
        print("No run directories with source found.")
        return

    for d in targets:
        print(f"\n=== {d} ===", flush=True)
        out = evaluate(d, do_test=not args.no_test)
        s = out["score"]
        print(f"score: {s['passed']}/{s['total']} ({s['rate']:.0%})", flush=True)


if __name__ == "__main__":
    main()
