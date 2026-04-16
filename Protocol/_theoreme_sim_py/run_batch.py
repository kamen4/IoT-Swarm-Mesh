"""Purpose: CLI entrypoint for running batch theorem simulation studies."""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime
from pathlib import Path

from src.research.batch_research_runner import run_batch_research_study
from src.research.comprehensive_study import (
    build_comprehensive_request,
    save_comprehensive_study_artifacts,
)
from src.research.run_artifacts import save_run_artifacts


def _build_quick_request() -> dict:
    return {
        "baseConfig": {
            "nodeCount": 34,
            "linkRadius": 195,
            "seed": 42,
            "maxRounds": 220,
        },
        "seedCount": 2,
        "optimizationIterations": 5,
        "roundsPerCheck": 220,
        "matrixText": "34x195,40x200",
        "parallelWorkers": 0,
    }


def _format_duration(seconds: float) -> str:
    whole = max(0, int(seconds))
    hours = whole // 3600
    minutes = (whole % 3600) // 60
    secs = whole % 60

    if hours > 0:
        return f"{hours:02d}:{minutes:02d}:{secs:02d}"
    return f"{minutes:02d}:{secs:02d}"


def _build_progress_callback() -> tuple:
    state = {
        "started": datetime.now(),
        "lastPercent": -1,
        "lastCompleted": -1,
    }

    bar_width = 30

    def _render(payload: dict) -> None:
        completed = int(payload.get("completed", 0) or 0)
        total = max(1, int(payload.get("total", 1) or 1))

        percent = int((completed * 100) / total)
        if completed != total and percent == state["lastPercent"]:
            return

        elapsed = (datetime.now() - state["started"]).total_seconds()
        eta_seconds = 0.0
        if completed > 0:
            eta_seconds = max(0.0, elapsed * (total - completed) / completed)

        filled = int((bar_width * completed) / total)
        filled = max(0, min(bar_width, filled))
        bar = "#" * filled + "-" * (bar_width - filled)

        line = (
            f"\rProgress [{bar}] {percent:3d}% "
            f"({completed}/{total}) ETA {_format_duration(eta_seconds)}"
        )
        sys.stdout.write(line)
        sys.stdout.flush()

        state["lastPercent"] = percent
        state["lastCompleted"] = completed

        if completed >= total:
            sys.stdout.write("\n")
            sys.stdout.flush()

    def _finish() -> None:
        if state["lastCompleted"] < 0:
            return
        if state["lastPercent"] < 100:
            sys.stdout.write("\n")
            sys.stdout.flush()

    return _render, _finish


def main() -> None:
    parser = argparse.ArgumentParser(description="Run theorem simulation batch study")
    parser.add_argument("--request", type=Path, help="Path to request JSON")
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help="Optional extra output JSON path",
    )
    parser.add_argument(
        "--res-root",
        type=Path,
        default=Path("res"),
        help="Root directory for run artifacts",
    )
    parser.add_argument(
        "--quick",
        action="store_true",
        help="Run a quick built-in smoke configuration",
    )
    parser.add_argument(
        "--comprehensive",
        action="store_true",
        help="Run comprehensive sensitivity profile and generate full research report",
    )
    parser.add_argument(
        "--no-progress",
        action="store_true",
        help="Disable live progress bar in terminal output",
    )

    args = parser.parse_args()

    if args.request:
        request = json.loads(args.request.read_text(encoding="utf-8"))
    elif args.comprehensive:
        request = build_comprehensive_request()
    else:
        request = _build_quick_request()

    run_request = json.loads(json.dumps(request, ensure_ascii=False))

    progress_finish = lambda: None
    if not args.no_progress:
        progress_callback, progress_finish = _build_progress_callback()
        run_request["onProgress"] = progress_callback

    started_at = datetime.now()
    try:
        result = run_batch_research_study(run_request)
    finally:
        progress_finish()
    finished_at = datetime.now()

    artifacts = save_run_artifacts(
        request=request,
        result=result,
        res_root=args.res_root,
        started_at=started_at,
        finished_at=finished_at,
    )

    comprehensive_artifacts = None
    if args.comprehensive:
        comprehensive_artifacts = save_comprehensive_study_artifacts(
            request=request,
            result=result,
            run_dir=artifacts["runDir"],
        )

    if args.output is not None:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(
            json.dumps(result, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )

    print(f"Saved run directory: {artifacts['runDir']}")
    print(f"Saved report JSON: {artifacts['fullReport']}")
    print(f"Saved summary markdown: {artifacts['summaryMd']}")
    print(f"Saved charts: {len(artifacts['charts'])}")
    if comprehensive_artifacts is not None:
        print(
            f"Saved comprehensive markdown: {comprehensive_artifacts['comprehensiveReport']}"
        )
        print(
            f"Saved comprehensive analysis JSON: {comprehensive_artifacts['analysisJson']}"
        )
    if args.output is not None:
        print(f"Saved extra output JSON: {args.output}")
    print(f"Topologies: {result['metadata']['topologyCount']}, total runs: {result['metadata']['totalRuns']}")


if __name__ == "__main__":
    main()
