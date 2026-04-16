"""Purpose: Parse topology matrix configuration into deterministic node/radius combinations."""

from __future__ import annotations

import re

from src.utils.math_utils import clamp, js_round


def _clamp_int(value, min_value: int, max_value: int) -> int:
    try:
        numeric = float(value)
    except (TypeError, ValueError):
        return min_value
    return js_round(clamp(numeric, min_value, max_value))


def _build_range(min_value: int, max_value: int, step: int) -> list[int]:
    safe_min = min(min_value, max_value)
    safe_max = max(min_value, max_value)
    safe_step = max(1, step)

    values: list[int] = []
    value = safe_min
    while value <= safe_max:
        values.append(value)
        value += safe_step

    if not values or values[-1] != safe_max:
        values.append(safe_max)

    return values


def _parse_matrix_text(matrix_text: str) -> list[dict]:
    if not matrix_text or not matrix_text.strip():
        return []

    tokens = [
        token.strip()
        for token in re.split(r"[\n,;]+", matrix_text)
        if token.strip()
    ]

    pairs: list[dict] = []
    for token in tokens:
        match = re.match(r"^(\d+)\s*[xX:]\s*(\d+)$", token)
        if not match:
            continue
        pairs.append(
            {
                "nodeCount": _clamp_int(match.group(1), 8, 320),
                "linkRadius": _clamp_int(match.group(2), 40, 600),
            }
        )

    return pairs


def build_topology_matrix(input_data: dict) -> list[dict]:
    from_text = _parse_matrix_text(input_data.get("matrixText", ""))

    if from_text:
        pairs = from_text
    else:
        nodes = _build_range(
            _clamp_int(input_data.get("nodeCountMin"), 8, 320),
            _clamp_int(input_data.get("nodeCountMax"), 8, 320),
            _clamp_int(input_data.get("nodeCountStep"), 1, 200),
        )
        radii = _build_range(
            _clamp_int(input_data.get("linkRadiusMin"), 40, 600),
            _clamp_int(input_data.get("linkRadiusMax"), 40, 600),
            _clamp_int(input_data.get("linkRadiusStep"), 1, 200),
        )

        pairs = []
        for node_count in nodes:
            for link_radius in radii:
                pairs.append({"nodeCount": node_count, "linkRadius": link_radius})

    dedup: dict[str, dict] = {}
    for pair in pairs:
        key = f"{pair['nodeCount']}x{pair['linkRadius']}"
        if key not in dedup:
            dedup[key] = {
                "id": key,
                "nodeCount": pair["nodeCount"],
                "linkRadius": pair["linkRadius"],
                "label": f"N={pair['nodeCount']}, R={pair['linkRadius']}",
            }

    return list(dedup.values())
