"""Purpose: Write lightweight SVG charts without external plotting dependencies."""

from __future__ import annotations

import html
from pathlib import Path


def _safe_label(value: str) -> str:
    return html.escape(str(value))


def _write_text(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8")


def write_bar_chart(
    path: Path,
    *,
    title: str,
    entries: list[tuple[str, float]],
    y_label: str,
    y_max: float | None = None,
    color: str = "#3f7ae0",
) -> None:
    width = 1100
    height = 640
    left = 80
    right = 30
    top = 80

    plot_width = width - left - right

    if not entries:
        svg = (
            f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>"
            "<rect width='100%' height='100%' fill='white'/>"
            f"<text x='{width / 2}' y='60' text-anchor='middle' font-size='28' font-family='Arial'>"
            f"{_safe_label(title)}</text>"
            f"<text x='{width / 2}' y='{height / 2}' text-anchor='middle' font-size='20' font-family='Arial' fill='#666'>"
            "No data</text>"
            "</svg>"
        )
        _write_text(path, svg)
        return

    def shorten(value: str, max_chars: int) -> str:
        text = str(value)
        limit = int(max_chars or 0)
        if limit <= 0:
            return ""
        if len(text) <= limit:
            return text
        if limit <= 6:
            return text[:limit]
        return text[: max(1, limit - 3)] + "..."

    count = len(entries)
    slot = plot_width / max(1, count)

    x_label_font = 13
    approx_char_w = x_label_font * 0.6
    max_label_len = max(len(str(label)) for label, _ in entries)
    rotate_labels = count >= 12 or (max_label_len * approx_char_w > slot * 1.05)

    if rotate_labels:
        x_label_font = 12
        approx_char_w = x_label_font * 0.6

    bottom = 230 if rotate_labels else 140
    plot_height = height - top - bottom

    values = [max(0.0, float(value)) for _, value in entries]
    max_value = max(values) if values else 1.0
    max_axis = max_value if y_max is None else max(float(y_max), 1e-9)
    max_axis = max(max_axis, 1e-9)

    bar_width = min(70.0, slot * 0.68)
    label_y = top + plot_height + (70 if rotate_labels else 24)

    parts: list[str] = []
    parts.append(f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>")
    parts.append("<rect width='100%' height='100%' fill='white'/>")
    parts.append(
        f"<text x='{width / 2}' y='46' text-anchor='middle' font-size='28' font-family='Arial'>{_safe_label(title)}</text>"
    )

    # Axes
    parts.append(
        f"<line x1='{left}' y1='{top + plot_height}' x2='{left + plot_width}' y2='{top + plot_height}' stroke='#222' stroke-width='2'/>"
    )
    parts.append(
        f"<line x1='{left}' y1='{top}' x2='{left}' y2='{top + plot_height}' stroke='#222' stroke-width='2'/>"
    )

    tick_count = 5
    for i in range(tick_count + 1):
        ratio = i / tick_count
        y = top + plot_height - ratio * plot_height
        value = ratio * max_axis
        parts.append(
            f"<line x1='{left - 8}' y1='{y:.1f}' x2='{left}' y2='{y:.1f}' stroke='#333' stroke-width='1'/>"
        )
        parts.append(
            f"<text x='{left - 12}' y='{y + 5:.1f}' text-anchor='end' font-size='14' font-family='Arial' fill='#333'>{value:.1f}</text>"
        )

    parts.append(
        f"<text x='24' y='{top + plot_height / 2:.1f}' transform='rotate(-90 24 {top + plot_height / 2:.1f})' text-anchor='middle' font-size='16' font-family='Arial'>{_safe_label(y_label)}</text>"
    )

    for index, (label, value) in enumerate(entries):
        clamped = max(0.0, float(value))
        bar_height = (clamped / max_axis) * plot_height
        x_center = left + slot * (index + 0.5)
        x = x_center - bar_width / 2
        y = top + plot_height - bar_height

        parts.append(
            f"<rect x='{x:.2f}' y='{y:.2f}' width='{bar_width:.2f}' height='{bar_height:.2f}' fill='{color}' opacity='0.9'/>"
        )
        parts.append(
            f"<text x='{x_center:.2f}' y='{y - 8:.2f}' text-anchor='middle' font-size='13' font-family='Arial' fill='#222'>{clamped:.2f}</text>"
        )

        max_chars = (
            min(24, max(6, int(slot / max(1e-6, approx_char_w)) + 8))
            if rotate_labels
            else min(20, max(4, int(slot / max(1e-6, approx_char_w)) - 1))
        )
        short = shorten(str(label), max_chars)

        if rotate_labels:
            parts.append(
                f"<text x='{x_center:.2f}' y='{label_y:.2f}' text-anchor='end' font-size='{x_label_font}' font-family='Arial' fill='#333' transform='rotate(-45 {x_center:.2f} {label_y:.2f})'>{_safe_label(short)}</text>"
            )
        else:
            parts.append(
                f"<text x='{x_center:.2f}' y='{label_y:.2f}' text-anchor='middle' font-size='{x_label_font}' font-family='Arial' fill='#333'>{_safe_label(short)}</text>"
            )

    parts.append("</svg>")
    _write_text(path, "".join(parts))


def write_line_chart(
    path: Path,
    *,
    title: str,
    series: dict[str, list[float]],
    y_label: str,
) -> None:
    width = 1200
    height = 680
    left = 90
    right = 30
    top = 90
    bottom = 100

    plot_width = width - left - right
    plot_height = height - top - bottom

    cleaned = {
        name: [float(value) for value in values]
        for name, values in series.items()
        if values
    }

    if not cleaned:
        svg = (
            f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>"
            "<rect width='100%' height='100%' fill='white'/>"
            f"<text x='{width / 2}' y='60' text-anchor='middle' font-size='28' font-family='Arial'>"
            f"{_safe_label(title)}</text>"
            f"<text x='{width / 2}' y='{height / 2}' text-anchor='middle' font-size='20' font-family='Arial' fill='#666'>"
            "No data</text>"
            "</svg>"
        )
        _write_text(path, svg)
        return

    max_len = max(len(values) for values in cleaned.values())
    x_steps = max(1, max_len - 1)

    all_values: list[float] = []
    for values in cleaned.values():
        all_values.extend(values)

    min_y = min(all_values)
    max_y = max(all_values)
    if min_y == max_y:
        min_y -= 1.0
        max_y += 1.0

    colors = ["#2f7ed8", "#e07a2f", "#2a9d62", "#c0392b", "#7d3c98"]

    def map_x(index: int) -> float:
        return left + (index / x_steps) * plot_width

    def map_y(value: float) -> float:
        ratio = (value - min_y) / (max_y - min_y)
        return top + plot_height - ratio * plot_height

    parts: list[str] = []
    parts.append(f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>")
    parts.append("<rect width='100%' height='100%' fill='white'/>")
    parts.append(
        f"<text x='{width / 2}' y='48' text-anchor='middle' font-size='28' font-family='Arial'>{_safe_label(title)}</text>"
    )

    parts.append(
        f"<line x1='{left}' y1='{top + plot_height}' x2='{left + plot_width}' y2='{top + plot_height}' stroke='#222' stroke-width='2'/>"
    )
    parts.append(
        f"<line x1='{left}' y1='{top}' x2='{left}' y2='{top + plot_height}' stroke='#222' stroke-width='2'/>"
    )

    tick_count = 5
    for i in range(tick_count + 1):
        ratio = i / tick_count
        y = top + plot_height - ratio * plot_height
        value = min_y + ratio * (max_y - min_y)
        parts.append(
            f"<line x1='{left - 8}' y1='{y:.1f}' x2='{left}' y2='{y:.1f}' stroke='#333' stroke-width='1'/>"
        )
        parts.append(
            f"<text x='{left - 14}' y='{y + 5:.1f}' text-anchor='end' font-size='14' font-family='Arial' fill='#333'>{value:.2f}</text>"
        )

    # X ticks
    x_tick_count = min(10, max_len)
    for i in range(x_tick_count):
        idx = int(round((i / max(1, x_tick_count - 1)) * (max_len - 1)))
        x = map_x(idx)
        parts.append(
            f"<line x1='{x:.1f}' y1='{top + plot_height}' x2='{x:.1f}' y2='{top + plot_height + 8}' stroke='#333' stroke-width='1'/>"
        )
        parts.append(
            f"<text x='{x:.1f}' y='{top + plot_height + 26}' text-anchor='middle' font-size='13' font-family='Arial' fill='#333'>R{idx + 1}</text>"
        )

    parts.append(
        f"<text x='30' y='{top + plot_height / 2:.1f}' transform='rotate(-90 30 {top + plot_height / 2:.1f})' text-anchor='middle' font-size='16' font-family='Arial'>{_safe_label(y_label)}</text>"
    )

    legend_x = left + 8
    legend_y = 58

    for idx, (name, values) in enumerate(cleaned.items()):
        color = colors[idx % len(colors)]
        points = " ".join(
            f"{map_x(i):.2f},{map_y(value):.2f}" for i, value in enumerate(values)
        )
        parts.append(
            f"<polyline fill='none' stroke='{color}' stroke-width='2.4' points='{points}'/>"
        )

        ly = legend_y + idx * 22
        parts.append(
            f"<line x1='{legend_x}' y1='{ly}' x2='{legend_x + 28}' y2='{ly}' stroke='{color}' stroke-width='4'/>"
        )
        parts.append(
            f"<text x='{legend_x + 36}' y='{ly + 5}' font-size='14' font-family='Arial' fill='#333'>{_safe_label(name)}</text>"
        )

    parts.append("</svg>")
    _write_text(path, "".join(parts))


def write_network_topology_chart(
    path: Path,
    *,
    title: str,
    topology: dict | None,
    node_charge_map: dict[int, float] | None = None,
    edge_weight_map: dict[str, float] | None = None,
    summary_lines: list[str] | None = None,
) -> None:
    width = 1200
    height = 900
    margin = 70

    nodes = [] if not topology else list(topology.get("nodes", []) or [])
    edges = [] if not topology else list(topology.get("edges", []) or [])

    if not nodes:
        svg = (
            f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>"
            "<rect width='100%' height='100%' fill='white'/>"
            f"<text x='{width / 2}' y='60' text-anchor='middle' font-size='28' font-family='Arial'>{_safe_label(title)}</text>"
            f"<text x='{width / 2}' y='{height / 2}' text-anchor='middle' font-size='20' font-family='Arial' fill='#666'>No topology data</text>"
            "</svg>"
        )
        _write_text(path, svg)
        return

    def _mix_channel(a: int, b: int, t: float) -> int:
        t_clamped = max(0.0, min(1.0, float(t)))
        return int(round(a + (b - a) * t_clamped))

    def _charge_fill(norm: float) -> str:
        t = max(0.0, min(1.0, float(norm)))
        if t < 0.5:
            local = t / 0.5
            r = _mix_channel(47, 248, local)
            g = _mix_channel(126, 218, local)
            b = _mix_channel(216, 95, local)
            return f"rgb({r},{g},{b})"

        local = (t - 0.5) / 0.5
        r = _mix_channel(248, 201, local)
        g = _mix_channel(218, 41, local)
        b = _mix_channel(95, 47, local)
        return f"rgb({r},{g},{b})"

    def _edge_key(a: int, b: int) -> str:
        return f"{a}:{b}" if a < b else f"{b}:{a}"

    charge_values: list[float] = []
    if node_charge_map:
        for node in nodes:
            node_id = int(node.get("id", -1))
            if node_id in node_charge_map:
                charge_values.append(float(node_charge_map[node_id]))

    charge_min = min(charge_values) if charge_values else 0.0
    charge_max = max(charge_values) if charge_values else 1.0
    charge_span = max(1e-9, charge_max - charge_min)

    edge_values: list[float] = []
    if edge_weight_map:
        for edge in edges:
            if isinstance(edge, list) and len(edge) == 2:
                a = int(edge[0])
                b = int(edge[1])
            elif isinstance(edge, dict):
                a = int(edge.get("a", -1))
                b = int(edge.get("b", -1))
            else:
                continue
            if a < 0 or b < 0:
                continue
            weight = float(edge_weight_map.get(_edge_key(a, b), 0.0))
            edge_values.append(weight)

    edge_min = min(edge_values) if edge_values else 0.0
    edge_max = max(edge_values) if edge_values else 1.0
    edge_span = max(1e-9, edge_max - edge_min)

    xs = [float(node.get("x", 0)) for node in nodes]
    ys = [float(node.get("y", 0)) for node in nodes]

    min_x = min(xs)
    max_x = max(xs)
    min_y = min(ys)
    max_y = max(ys)

    span_x = max(1e-6, max_x - min_x)
    span_y = max(1e-6, max_y - min_y)

    plot_width = width - 2 * margin
    plot_height = height - 2 * margin

    def map_x(value: float) -> float:
        return margin + ((value - min_x) / span_x) * plot_width

    def map_y(value: float) -> float:
        return margin + ((value - min_y) / span_y) * plot_height

    node_lookup = {int(node.get("id")): node for node in nodes}

    parts: list[str] = []
    parts.append(f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>")
    parts.append("<rect width='100%' height='100%' fill='white'/>")
    parts.append(
        f"<text x='{width / 2}' y='42' text-anchor='middle' font-size='28' font-family='Arial'>{_safe_label(title)}</text>"
    )

    # Draw edges first.
    for edge in edges:
        if isinstance(edge, list) and len(edge) == 2:
            a = int(edge[0])
            b = int(edge[1])
        elif isinstance(edge, dict):
            a = int(edge.get("a", -1))
            b = int(edge.get("b", -1))
        else:
            continue

        if a < 0 or b < 0:
            continue

        u = node_lookup.get(a)
        v = node_lookup.get(b)
        if not u or not v:
            continue

        x1 = map_x(float(u.get("x", 0)))
        y1 = map_y(float(u.get("y", 0)))
        x2 = map_x(float(v.get("x", 0)))
        y2 = map_y(float(v.get("y", 0)))

        stroke = "#b9c2cf"
        stroke_width = 1.2
        if edge_weight_map is not None:
            key = _edge_key(a, b)
            weight = float(edge_weight_map.get(key, 0.0))
            w_norm = (weight - edge_min) / edge_span if edge_span > 1e-9 else 0.5
            w_norm = max(0.0, min(1.0, w_norm))
            stroke_width = 0.9 + 2.7 * w_norm
            r = _mix_channel(194, 49, w_norm)
            g = _mix_channel(201, 98, w_norm)
            b_ch = _mix_channel(214, 165, w_norm)
            stroke = f"rgb({r},{g},{b_ch})"

        parts.append(
            f"<line x1='{x1:.2f}' y1='{y1:.2f}' x2='{x2:.2f}' y2='{y2:.2f}' stroke='{stroke}' stroke-width='{stroke_width:.2f}'/>"
        )

    # Draw nodes.
    node_count = max(1, len(nodes))
    show_labels = node_count <= 70

    for node in nodes:
        node_id = int(node.get("id", -1))
        x = map_x(float(node.get("x", 0)))
        y = map_y(float(node.get("y", 0)))
        is_gateway = bool(node.get("isGateway", False))

        has_charge = node_charge_map is not None and node_id in (node_charge_map or {})
        if is_gateway:
            radius = 8.0
            fill = "#cc2f2f"
            stroke = "#5b1f1f"
        elif has_charge:
            charge_value = float((node_charge_map or {}).get(node_id, 0.0))
            c_norm = (charge_value - charge_min) / charge_span if charge_span > 1e-9 else 0.5
            c_norm = max(0.0, min(1.0, c_norm))
            radius = 4.2 + 3.4 * c_norm
            fill = _charge_fill(c_norm)
            stroke = "#30455f"
        else:
            radius = 4.2
            fill = "#2f7ed8"
            stroke = "#1d4a86"

        parts.append(
            f"<circle cx='{x:.2f}' cy='{y:.2f}' r='{radius:.2f}' fill='{fill}' stroke='{stroke}' stroke-width='1.2'/>"
        )

        if show_labels or is_gateway:
            parts.append(
                f"<text x='{x + 6:.2f}' y='{y - 6:.2f}' font-size='10' font-family='Arial' fill='#333'>{node_id}</text>"
            )

        if has_charge and (is_gateway or node_count <= 55):
            q_value = float((node_charge_map or {}).get(node_id, 0.0))
            parts.append(
                f"<text x='{x + 6:.2f}' y='{y + 12:.2f}' font-size='9' font-family='Arial' fill='#444'>q={q_value:.1f}</text>"
            )

    # Legend
    legend_x = width - 260
    legend_y = 72
    legend_height = 74
    if node_charge_map:
        legend_height += 16
    if edge_weight_map:
        legend_height += 16
    if summary_lines:
        legend_height += 16 * len(summary_lines)

    parts.append(
        f"<rect x='{legend_x}' y='{legend_y}' width='220' height='{legend_height}' rx='8' fill='white' stroke='#cfd5dd'/>"
    )
    parts.append(
        f"<circle cx='{legend_x + 18}' cy='{legend_y + 22}' r='6' fill='#cc2f2f' stroke='#5b1f1f' stroke-width='1.2'/>"
    )
    parts.append(
        f"<text x='{legend_x + 34}' y='{legend_y + 26}' font-size='13' font-family='Arial' fill='#333'>Gateway</text>"
    )
    parts.append(
        f"<circle cx='{legend_x + 18}' cy='{legend_y + 48}' r='4.2' fill='#2f7ed8' stroke='#1d4a86' stroke-width='1.2'/>"
    )
    parts.append(
        f"<text x='{legend_x + 34}' y='{legend_y + 52}' font-size='13' font-family='Arial' fill='#333'>Regular node</text>"
    )

    line_y = legend_y + 68
    parts.append(
        f"<text x='{legend_x + 12}' y='{line_y}' font-size='12' font-family='Arial' fill='#666'>Nodes: {len(nodes)}, Edges: {len(edges)}</text>"
    )
    line_y += 16

    if node_charge_map:
        parts.append(
            f"<text x='{legend_x + 12}' y='{line_y}' font-size='12' font-family='Arial' fill='#666'>Charge range: {charge_min:.1f}..{charge_max:.1f}</text>"
        )
        line_y += 16

    if edge_weight_map:
        parts.append(
            f"<text x='{legend_x + 12}' y='{line_y}' font-size='12' font-family='Arial' fill='#666'>Edge weight: {edge_min:.2f}..{edge_max:.2f}</text>"
        )
        line_y += 16

    for item in summary_lines or []:
        parts.append(
            f"<text x='{legend_x + 12}' y='{line_y}' font-size='12' font-family='Arial' fill='#666'>{_safe_label(item)}</text>"
        )
        line_y += 16

    parts.append("</svg>")
    _write_text(path, "".join(parts))


def write_signed_bar_chart(
    path: Path,
    *,
    title: str,
    entries: list[tuple[str, float]],
    y_label: str,
    y_limit: float = 1.0,
) -> None:
    width = 1200
    height = 700
    left = 90
    right = 30
    top = 90

    plot_width = width - left - right

    if not entries:
        svg = (
            f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>"
            "<rect width='100%' height='100%' fill='white'/>"
            f"<text x='{width / 2}' y='60' text-anchor='middle' font-size='28' font-family='Arial'>"
            f"{_safe_label(title)}</text>"
            f"<text x='{width / 2}' y='{height / 2}' text-anchor='middle' font-size='20' font-family='Arial' fill='#666'>"
            "No data</text>"
            "</svg>"
        )
        _write_text(path, svg)
        return

    def shorten(value: str, max_chars: int) -> str:
        text = str(value)
        limit = int(max_chars or 0)
        if limit <= 0:
            return ""
        if len(text) <= limit:
            return text
        if limit <= 6:
            return text[:limit]
        return text[: max(1, limit - 3)] + "..."

    limit = max(0.01, float(y_limit))
    count = len(entries)
    slot = plot_width / max(1, count)

    x_label_font = 12
    approx_char_w = x_label_font * 0.6
    max_label_len = max(len(str(label)) for label, _ in entries)
    rotate_labels = count >= 12 or (max_label_len * approx_char_w > slot * 1.05)

    bottom = 230 if rotate_labels else 140
    plot_height = height - top - bottom

    bar_width = min(66.0, slot * 0.62)
    label_y = top + plot_height + (70 if rotate_labels else 24)

    def map_y(value: float) -> float:
        clamped = max(-limit, min(limit, float(value)))
        ratio = (clamped + limit) / (2 * limit)
        return top + plot_height - ratio * plot_height

    y_zero = map_y(0.0)

    parts: list[str] = []
    parts.append(f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>")
    parts.append("<rect width='100%' height='100%' fill='white'/>")
    parts.append(
        f"<text x='{width / 2}' y='48' text-anchor='middle' font-size='28' font-family='Arial'>{_safe_label(title)}</text>"
    )

    parts.append(
        f"<line x1='{left}' y1='{top + plot_height}' x2='{left + plot_width}' y2='{top + plot_height}' stroke='#222' stroke-width='2'/>"
    )
    parts.append(
        f"<line x1='{left}' y1='{top}' x2='{left}' y2='{top + plot_height}' stroke='#222' stroke-width='2'/>"
    )
    parts.append(
        f"<line x1='{left}' y1='{y_zero:.2f}' x2='{left + plot_width}' y2='{y_zero:.2f}' stroke='#666' stroke-width='1.4' stroke-dasharray='6 4'/>"
    )

    tick_count = 4
    for i in range(tick_count * 2 + 1):
        value = -limit + (i / (tick_count * 2)) * (2 * limit)
        y = map_y(value)
        parts.append(
            f"<line x1='{left - 8}' y1='{y:.2f}' x2='{left}' y2='{y:.2f}' stroke='#333' stroke-width='1'/>"
        )
        parts.append(
            f"<text x='{left - 12}' y='{y + 5:.2f}' text-anchor='end' font-size='13' font-family='Arial' fill='#333'>{value:.2f}</text>"
        )

    parts.append(
        f"<text x='28' y='{top + plot_height / 2:.1f}' transform='rotate(-90 28 {top + plot_height / 2:.1f})' text-anchor='middle' font-size='16' font-family='Arial'>{_safe_label(y_label)}</text>"
    )

    for index, (label, raw_value) in enumerate(entries):
        value = max(-limit, min(limit, float(raw_value)))
        x_center = left + slot * (index + 0.5)
        x = x_center - bar_width / 2
        y = map_y(value)
        bar_top = min(y, y_zero)
        bar_height = abs(y_zero - y)
        color = "#2f7ed8" if value >= 0 else "#d26a36"

        parts.append(
            f"<rect x='{x:.2f}' y='{bar_top:.2f}' width='{bar_width:.2f}' height='{bar_height:.2f}' fill='{color}' opacity='0.92'/>"
        )

        value_label_y = y - 8 if value >= 0 else y + 18
        parts.append(
            f"<text x='{x_center:.2f}' y='{value_label_y:.2f}' text-anchor='middle' font-size='12' font-family='Arial' fill='#222'>{value:.3f}</text>"
        )

        max_chars = min(24, max(6, int(slot / max(1e-6, approx_char_w)) + 8))
        short = shorten(str(label), max_chars)

        if rotate_labels:
            parts.append(
                f"<text x='{x_center:.2f}' y='{label_y:.2f}' text-anchor='end' font-size='{x_label_font}' font-family='Arial' fill='#333' transform='rotate(-45 {x_center:.2f} {label_y:.2f})'>{_safe_label(short)}</text>"
            )
        else:
            parts.append(
                f"<text x='{x_center:.2f}' y='{label_y:.2f}' text-anchor='middle' font-size='{x_label_font}' font-family='Arial' fill='#333'>{_safe_label(short)}</text>"
            )

    parts.append("</svg>")
    _write_text(path, "".join(parts))


def write_heatmap_chart(
    path: Path,
    *,
    title: str,
    row_labels: list[str],
    column_labels: list[str],
    matrix: list[list[float]],
    vmin: float = -1.0,
    vmax: float = 1.0,
) -> None:
    if not row_labels or not column_labels:
        write_bar_chart(path, title=title, entries=[], y_label="")
        return

    cell_w = 68
    cell_h = 30
    left = 260
    top = 140
    right = 40
    bottom = 40

    width = left + len(column_labels) * cell_w + right
    height = top + len(row_labels) * cell_h + bottom

    span = max(1e-9, float(vmax) - float(vmin))

    def _mix(a: int, b: int, t: float) -> int:
        t_clamped = max(0.0, min(1.0, t))
        return int(round(a + (b - a) * t_clamped))

    def _cell_color(value: float) -> str:
        normalized = (float(value) - float(vmin)) / span
        normalized = max(0.0, min(1.0, normalized))

        if normalized >= 0.5:
            t = (normalized - 0.5) / 0.5
            r = _mix(245, 46, t)
            g = _mix(245, 110, t)
            b = _mix(245, 196, t)
            return f"rgb({r},{g},{b})"

        t = normalized / 0.5
        r = _mix(206, 245, t)
        g = _mix(66, 245, t)
        b = _mix(66, 245, t)
        return f"rgb({r},{g},{b})"

    parts: list[str] = []
    parts.append(f"<svg xmlns='http://www.w3.org/2000/svg' width='{width}' height='{height}'>")
    parts.append("<rect width='100%' height='100%' fill='white'/>")
    parts.append(
        f"<text x='{width / 2:.2f}' y='48' text-anchor='middle' font-size='26' font-family='Arial'>{_safe_label(title)}</text>"
    )

    for row_idx, row_name in enumerate(row_labels):
        y = top + row_idx * cell_h
        parts.append(
            f"<text x='{left - 10}' y='{y + 20}' text-anchor='end' font-size='12' font-family='Arial' fill='#333'>{_safe_label(row_name)}</text>"
        )

        values = matrix[row_idx] if row_idx < len(matrix) else []
        for col_idx, _ in enumerate(column_labels):
            x = left + col_idx * cell_w
            value = float(values[col_idx]) if col_idx < len(values) else 0.0
            color = _cell_color(value)
            parts.append(
                f"<rect x='{x}' y='{y}' width='{cell_w}' height='{cell_h}' fill='{color}' stroke='white' stroke-width='1'/>"
            )
            parts.append(
                f"<text x='{x + cell_w / 2:.2f}' y='{y + 20}' text-anchor='middle' font-size='11' font-family='Arial' fill='#222'>{value:.2f}</text>"
            )

    label_font = 12
    approx_char_w = label_font * 0.6
    max_chars = max(3, int(cell_w / max(1e-6, approx_char_w)) - 1)

    for col_idx, col_name in enumerate(column_labels):
        x = left + col_idx * cell_w + cell_w / 2

        raw = str(col_name)
        if len(raw) > max_chars:
            raw = raw[: max(1, max_chars - 3)] + "..." if max_chars >= 6 else raw[:max_chars]

        parts.append(
            f"<text x='{x:.2f}' y='{top - 12}' text-anchor='middle' font-size='{label_font}' font-family='Arial' fill='#333'>{_safe_label(raw)}</text>"
        )

    bar_x = left
    bar_y = top + len(row_labels) * cell_h + 18
    bar_w = min(420, len(column_labels) * cell_w)
    bar_h = 14

    gradient_id = "hm-grad"
    parts.append("<defs>")
    parts.append(
        f"<linearGradient id='{gradient_id}' x1='0%' y1='0%' x2='100%' y2='0%'>"
        "<stop offset='0%' stop-color='rgb(206,66,66)'/>"
        "<stop offset='50%' stop-color='rgb(245,245,245)'/>"
        "<stop offset='100%' stop-color='rgb(46,110,196)'/>"
        "</linearGradient>"
    )
    parts.append("</defs>")
    parts.append(
        f"<rect x='{bar_x}' y='{bar_y}' width='{bar_w}' height='{bar_h}' fill='url(#{gradient_id})' stroke='#bbb' stroke-width='0.8'/>"
    )
    parts.append(
        f"<text x='{bar_x}' y='{bar_y + 30}' font-size='11' font-family='Arial' fill='#333'>{vmin:.1f}</text>"
    )
    parts.append(
        f"<text x='{bar_x + bar_w / 2:.2f}' y='{bar_y + 30}' text-anchor='middle' font-size='11' font-family='Arial' fill='#333'>0.0</text>"
    )
    parts.append(
        f"<text x='{bar_x + bar_w:.2f}' y='{bar_y + 30}' text-anchor='end' font-size='11' font-family='Arial' fill='#333'>{vmax:.1f}</text>"
    )

    parts.append("</svg>")
    _write_text(path, "".join(parts))
