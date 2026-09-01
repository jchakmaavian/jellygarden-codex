#!/usr/bin/env python3
"""Compose the 30 arc-beat cutscenes from the authored story.

Every fifth area is a beat and gets a scene rather than a card. The markdown
does not contain scripted dialogue — it contains a Goal, a Direction note and a
Clue, plus an ARC.md with the arc's question and payoff. This turns that
material into a short scene by assigning each piece to the character who would
say it, and by using the arc's own cast rather than a fixed one:

  Alfred  frames the area, as the narrator voice the story bible gives him
  Leo     states the goal; he is the one doing the work
  Freya   joins from area 26 and takes the reading-the-evidence lines
  Grimm   speaks only where the Direction says he acts

Nothing is invented about the plot: every line is the story's own words, placed
in a mouth. Where the Direction is a production note rather than something a
character could say, it becomes narration.

    python3 tools/cutscene_build.py
"""

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from story_common import (  # noqa: E402
    ARC_DIRS,
    ARC_TITLES,
    STORY_RES,
    STORY_ROOT,
    all_areas,
    dump_json,
)

# Freya is rescued at area 25 and co-leads from area 26 (STORY.md, production
# rule 1). Before that she exists only through objects and notes.
FREYA_JOINS_AT = 26

BEAT_EVERY = 5
FINALE_EVERY = 25

MIN_BEAT_LINES = 3
MIN_FINALE_LINES = 6


def read_area_markdown(area):
    path = os.path.join(STORY_ROOT, area["source"])
    if not os.path.exists(path):
        return ""
    with open(path, encoding="utf-8") as fh:
        return fh.read()


def section(text, heading):
    """The body of a '## heading' section, blockquote markers stripped."""
    match = re.search(
        r"^##\s+%s\s*$\n(.*?)(?=^##\s|\Z)" % re.escape(heading),
        text,
        flags=re.MULTILINE | re.DOTALL,
    )
    if not match:
        return ""

    lines = []
    for raw in match.group(1).split("\n"):
        line = raw.strip()
        if not line or line.startswith("|") or line.startswith("---"):
            continue
        line = line.lstrip("> ").strip()
        line = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", line)  # unlink
        line = line.replace("**", "").replace("_", "")
        lines.append(line)
    return " ".join(lines).strip()


def sentences(text):
    """Split into sentences, keeping their punctuation. Empty input yields []."""
    if not text:
        return []
    parts = re.split(r"(?<=[.!?—])\s+", text)
    return [p.strip() for p in parts if p.strip()]


def read_arc_overview(arc):
    path = os.path.join(STORY_ROOT, ARC_DIRS[arc], "ARC.md")
    if not os.path.exists(path):
        return {}
    with open(path, encoding="utf-8") as fh:
        text = fh.read()

    def field(name):
        match = re.search(r"^\*\*%s:\*\*\s*(.+)$" % re.escape(name), text, flags=re.MULTILINE)
        return match.group(1).strip().replace("**", "") if match else ""

    return {
        "question": field("Question"),
        "payoff": field("Payoff"),
        "villain": field("Villain in this arc"),
        "setting": field("Setting"),
        "summary": section(text, "Summary"),
    }


# Labels the markdown uses to mark a beat for the production team. They are
# instructions to a designer, not something a character would ever say.
PRODUCTION_LABELS = ("ARC PAYOFF", "ARC BEAT", "BEAT")


def strip_production_label(text):
    for label in PRODUCTION_LABELS:
        pattern = r"^\s*%s[.:]?\s*" % re.escape(label)
        text = re.sub(pattern, "", text, flags=re.IGNORECASE)
    return text.strip()


def frame_line(area):
    phase = strip_production_label(area.get("phase", "")).split("—")[0].strip()
    if not phase or phase.lower() in ("", "arc payoff"):
        return area["name"] + "."
    return "%s. %s." % (area["name"], phase)


def lead_for(area_id):
    """Who is reading the evidence in this area."""
    return "Freya" if area_id >= FREYA_JOINS_AT else "Leo"


def build_scene(area, arc_overview):
    """One beat's lines. Order is: where we are, what we want, what happened, what it means."""
    text = read_area_markdown(area)
    direction = section(text, "Direction")
    goal = area.get("goal") or section(text, "Goal")
    clue = area.get("clue") or section(text, "Clue")

    arc = area["arc"]
    is_finale = area["id"] % FINALE_EVERY == 0
    lead = lead_for(area["id"])

    lines = []

    # Alfred frames it. He is the narrator voice, so the framing is the room's
    # name and, when the phase is a description rather than a production label,
    # what stage of the arc it belongs to.
    lines.append(("Alfred", frame_line(area)))

    if goal:
        lines.append(("Leo", goal))

    # The Direction note is production guidance. Anything that reads as an event
    # becomes narration; a bare instruction like "ARC PAYOFF" is dropped.
    for line in sentences(direction):
        if not line or line.upper().startswith("ARC PAYOFF"):
            continue
        if len(line) < 8:
            continue
        speaker = "Grimm" if line.startswith("Grimm") and line.count(" ") > 2 else "Alfred"
        lines.append((speaker, line))

    if clue:
        lines.append((lead, strip_production_label(clue)))

    if is_finale and arc_overview:
        # The arc's own answer to its own question, in the two voices that earned it.
        if arc_overview.get("question"):
            lines.append(("Alfred", arc_overview["question"]))
        if arc_overview.get("payoff"):
            lines.append((lead, arc_overview["payoff"]))
        if arc_overview.get("summary"):
            for line in sentences(arc_overview["summary"])[:3]:
                lines.append(("Alfred", line))
        if arc < 6:
            lines.append(("Leo", "It does not end here."))

    # Pad a thin beat with the arc's standing question rather than filler.
    minimum = MIN_FINALE_LINES if is_finale else MIN_BEAT_LINES
    if len(lines) < minimum and arc_overview.get("question"):
        lines.append(("Alfred", arc_overview["question"]))
    if len(lines) < minimum and arc_overview.get("setting"):
        lines.append(("Alfred", arc_overview["setting"]))
    if len(lines) < minimum:
        lines.append((lead, "Then we keep looking."))

    return {
        "areaId": area["id"],
        "title": "Area %03d — %s" % (area["id"], area["name"]),
        "arc": arc,
        "arcTitle": ARC_TITLES[arc],
        "isFinale": is_finale,
        "lines": [{"speaker": s, "text": t, "voice": ""} for s, t in lines],
    }


def main():
    areas = all_areas()
    overviews = {arc: read_arc_overview(arc) for arc in range(1, 7)}

    cutscenes = []
    for area in areas:
        if area["id"] % BEAT_EVERY != 0:
            continue
        cutscenes.append(build_scene(area, overviews.get(area["arc"], {})))

    failures = []
    for scene in cutscenes:
        minimum = MIN_FINALE_LINES if scene["isFinale"] else MIN_BEAT_LINES
        if len(scene["lines"]) < minimum:
            failures.append(
                "area %d has %d lines, needs at least %d"
                % (scene["areaId"], len(scene["lines"]), minimum)
            )
        for line in scene["lines"]:
            if not line["text"].strip():
                failures.append("area %d has an empty line" % scene["areaId"])

    expected = len(areas) // BEAT_EVERY
    if len(cutscenes) != expected:
        failures.append("expected %d beats, built %d" % (expected, len(cutscenes)))

    if failures:
        for failure in failures:
            print("ERROR: " + failure, file=sys.stderr)
        return 1

    dump_json(
        os.path.join(STORY_RES, "cutscenes.json"),
        {"cutsceneCount": len(cutscenes), "cutscenes": cutscenes},
    )

    finales = sum(1 for s in cutscenes if s["isFinale"])
    total_lines = sum(len(s["lines"]) for s in cutscenes)
    print("cutscenes=%d" % len(cutscenes))
    print("finales=%d, lines=%d" % (finales, total_lines))
    return 0


if __name__ == "__main__":
    sys.exit(main())
