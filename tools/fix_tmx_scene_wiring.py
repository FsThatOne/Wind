#!/usr/bin/env python3
"""Fix TMX scene wiring: update target_scene to full res:// paths,
add entry markers, and wire existing scenes (main_hall, cliff_cave)."""

import re
import os

BASE = "/Users/bytedance/my-game/feng-zhi/assets/maps"

# Scene name → full .tscn path mapping
SCENE_PATHS = {
    "main_hall": "res://scenes/main_hall/MainHall.tscn",
    "back_mountain_cliff_cave": "res://scenes/back_mountain_cliff_cave/BackMountainCliffCave.tscn",
    "back_mountain_path": "res://scenes/back_mountain_path/BackMountainPath.tscn",
    "mountain_gate": "res://scenes/mountain_gate/MountainGate.tscn",
    "study": "res://scenes/study/Study.tscn",
    "living_quarter": "res://scenes/living_quarter/LivingQuarter.tscn",
    "alchemy_room": "res://scenes/alchemy_room/AlchemyRoom.tscn",
}

def replace_target_scenes(content):
    """Replace short target_scene names with full res:// paths."""
    for short_name, full_path in SCENE_PATHS.items():
        content = content.replace(
            f'name="target_scene" value="{short_name}"',
            f'name="target_scene" value="{full_path}"'
        )
    return content

def add_entry_marker_object(next_obj_id, name, tile_x, tile_y):
    """Generate an entry marker object XML snippet."""
    px = tile_x * 64.0
    py = tile_y * 32.0
    return (
        f'<object id="{next_obj_id}" name="{name}" type="entry" '
        f'x="{px}" y="{py}" width="64" height="32">'
        f'<properties>'
        f'<property name="id" value="{name}" />'
        f'<property name="type" value="entry" />'
        f'<property name="enabled" type="bool" value="true" />'
        f'<property name="tile_x" type="int" value="{tile_x}" />'
        f'<property name="tile_y" type="int" value="{tile_y}" />'
        f'</properties></object>'
    )

# Entry markers needed per scene (name → tile position)
ENTRY_MARKERS = {
    "main_hall": {
        "entry_from_gate": (7, 11),      # bottom exit, same as exit_to_courtyard
        "entry_from_study": (5, 5),      # near tea table area
        "entry_from_living": (3, 7),     # left side
        "entry_from_training": (10, 7),  # right side
    },
    "back_mountain_cliff_cave": {
        "entry_from_path": (1, 8),       # same as exit_to_back_mountain
    },
    "back_mountain_path": {
        "entry_from_gate": (7, 2),       # top, same as exit_to_mountain_gate
        "entry_from_cliff_cave": (5, 13),  # bottom, same as exit_to_cliff_cave
    },
    "mountain_gate": {
        "entry_from_path": (7, 3),       # top, same as exit_to_back_mountain
        "entry_from_main_hall": (7, 12), # bottom, same as exit_to_courtyard
        "entry_from_training": (5, 7),   # left side
    },
    "study": {
        "entry_from_main_hall": (7, 11), # same as exit_to_main_hall
    },
    "living_quarter": {
        "entry_from_main_hall": (7, 11), # same as exit_to_courtyard
    },
    "alchemy_room": {
        "entry_from_main_hall": (7, 12), # same as exit_to_courtyard
    },
}

# Additional exit markers to add to existing scenes
EXTRA_EXITS = {
    "main_hall": [
        ("exit_to_study", "exit", 5, 3, "res://scenes/study/Study.tscn", "entry_from_main_hall"),
        ("exit_to_living_quarter", "exit", 3, 7, "res://scenes/living_quarter/LivingQuarter.tscn", "entry_from_main_hall"),
        ("exit_to_alchemy_room", "exit", 10, 7, "res://scenes/alchemy_room/AlchemyRoom.tscn", "entry_from_main_hall"),
    ],
    "back_mountain_cliff_cave": [],
    "mountain_gate": [
        ("exit_to_alchemy_room", "exit", 5, 7, "res://scenes/alchemy_room/AlchemyRoom.tscn", "entry_from_main_hall"),
    ],
}

def add_exit_object(next_obj_id, name, etype, tile_x, tile_y, target_scene, entry_marker):
    px = tile_x * 64.0
    py = tile_y * 32.0
    return (
        f'<object id="{next_obj_id}" name="{name}" type="{etype}" '
        f'x="{px}" y="{py}" width="64" height="32">'
        f'<properties>'
        f'<property name="id" value="{name}" />'
        f'<property name="type" value="{etype}" />'
        f'<property name="enabled" type="bool" value="true" />'
        f'<property name="target_scene" value="{target_scene}" />'
        f'<property name="entry_marker" value="{entry_marker}" />'
        f'<property name="tile_x" type="int" value="{tile_x}" />'
        f'<property name="tile_y" type="int" value="{tile_y}" />'
        f'</properties></object>'
    )

def get_next_object_id(content):
    """Find the nextobjectid from the map header."""
    m = re.search(r'nextobjectid="(\d+)"', content)
    return int(m.group(1)) if m else 100

def update_next_object_id(content, new_id):
    return re.sub(r'nextobjectid="\d+"', f'nextobjectid="{new_id}"', content)

def process_tmx(scene_name, tmx_path):
    if not os.path.exists(tmx_path):
        print(f"  SKIP (not found): {tmx_path}")
        return

    with open(tmx_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Step 1: Replace short target_scene names with full paths
    content = replace_target_scenes(content)

    # Step 2: Add target_scene/entry_marker to existing exits that lack them
    # For main_hall: exit_to_courtyard → mountain_gate
    if scene_name == "main_hall" and 'name="exit_to_courtyard"' in content:
        if 'target_scene' not in content.split('exit_to_courtyard')[1].split('</object>')[0]:
            content = content.replace(
                '<property name="tile_x" type="int" value="7" /><property name="tile_y" type="int" value="11" /></properties></object></objectgroup>',
                '<property name="target_scene" value="res://scenes/mountain_gate/MountainGate.tscn" />'
                '<property name="entry_marker" value="entry_from_main_hall" />'
                '<property name="tile_x" type="int" value="7" /><property name="tile_y" type="int" value="11" /></properties></object></objectgroup>'
            )

    if scene_name == "back_mountain_cliff_cave" and 'name="exit_to_back_mountain"' in content:
        if 'target_scene' not in content.split('exit_to_back_mountain')[1].split('</object>')[0]:
            content = content.replace(
                '<property name="tile_x" type="int" value="1" /><property name="tile_y" type="int" value="8" /></properties></object>',
                '<property name="target_scene" value="res://scenes/back_mountain_path/BackMountainPath.tscn" />'
                '<property name="entry_marker" value="entry_from_cliff_cave" />'
                '<property name="tile_x" type="int" value="1" /><property name="tile_y" type="int" value="8" /></properties></object>',
                1  # only first occurrence
            )

    # Step 3: Add entry markers and extra exits
    next_id = get_next_object_id(content)
    new_objects = []

    # Entry markers
    if scene_name in ENTRY_MARKERS:
        for marker_name, (tx, ty) in ENTRY_MARKERS[scene_name].items():
            if f'name="{marker_name}"' not in content:
                new_objects.append(add_entry_marker_object(next_id, marker_name, tx, ty))
                next_id += 1

    # Extra exits
    if scene_name in EXTRA_EXITS:
        for (name, etype, tx, ty, target, entry) in EXTRA_EXITS[scene_name]:
            if f'name="{name}"' not in content:
                new_objects.append(add_exit_object(next_id, name, etype, tx, ty, target, entry))
                next_id += 1

    if new_objects:
        insert_point = '</objectgroup></map>'
        new_xml = ''.join(new_objects) + insert_point
        content = content.replace(insert_point, new_xml)
        content = update_next_object_id(content, next_id)

    with open(tmx_path, 'w', encoding='utf-8') as f:
        f.write(content)
    print(f"  OK: {tmx_path} ({len(new_objects)} objects added)")

def main():
    scenes = [
        "main_hall", "back_mountain_cliff_cave", "back_mountain_path",
        "mountain_gate", "study", "living_quarter", "alchemy_room"
    ]

    for scene in scenes:
        print(f"\nProcessing: {scene}")
        day_tmx = os.path.join(BASE, scene, "maps", f"{scene}_day.tmx")
        night_tmx = os.path.join(BASE, scene, "maps", f"{scene}_night.tmx")
        process_tmx(scene, day_tmx)
        process_tmx(scene, night_tmx)

if __name__ == "__main__":
    main()
