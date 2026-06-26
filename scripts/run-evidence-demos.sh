#!/usr/bin/env bash
# 自动录制 4 个 cu-visual-evidence demo 场景的视频证据
# 使用 Godot --write-movie 模式 + AutoEvidencePlayer 自动输入
# 用法: ./scripts/run-evidence-demos.sh [场景编号]

set -euo pipefail

GODOT="/Applications/Godot_mono.app/Contents/MacOS/Godot"
PROJECT="/Users/bytedance/my-game/feng-zhi"
OUTPUT_DIR="/Users/bytedance/my-game/production/qa/evidence/media"

mkdir -p "$OUTPUT_DIR"

declare -a SCENES=(
    "scenes/vs/demo/battle_demo_cu004.tscn"
    "scenes/vs/demo/battle_demo_cu005.tscn"
    "scenes/vs/demo/battle_demo_cu006.tscn"
    "scenes/vs/demo/battle_demo_cu008.tscn"
)
declare -a NAMES=("cu-004" "cu-005" "cu-006" "cu-008")
declare -a QUIT_FRAMES=(420 480 480 300)

record_scene() {
    local idx=$1
    local scene="${SCENES[$idx]}"
    local name="${NAMES[$idx]}"
    local frames="${QUIT_FRAMES[$idx]}"
    local output="$OUTPUT_DIR/${name}-evidence.avi"

    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  录制 ${name} → ${output}"
    echo "  场景: ${scene}"
    echo "  帧数: ${frames} (@ 30fps ≈ $((frames/30))s)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""

    "$GODOT" --path "$PROJECT" \
        --write-movie "$output" \
        --fixed-fps 30 \
        --quit-after "$frames" \
        --resolution 1280x720 \
        "$scene" 2>&1 | grep -v "IMKCFRunLoopWakeUpReliable" || true

    if [ -f "$output" ]; then
        local size
        size=$(du -h "$output" | cut -f1)
        echo "  ✓ 录制完成: ${output} (${size})"
    else
        echo "  ✗ 录制失败: 文件未生成"
    fi
}

if [ "${1:-}" != "" ]; then
    idx=$((${1} - 1))
    if [ $idx -ge 0 ] && [ $idx -lt 4 ]; then
        record_scene $idx
    else
        echo "用法: $0 [1-4]"
        echo "  1 = cu-004  2 = cu-005  3 = cu-006  4 = cu-008"
        exit 1
    fi
else
    echo "╔══════════════════════════════════════════╗"
    echo "║  自动录制全部 4 个 demo 场景视频证据    ║"
    echo "║  输出: production/qa/evidence/media/    ║"
    echo "╚══════════════════════════════════════════╝"

    for i in 0 1 2 3; do
        record_scene $i
    done

    echo ""
    echo "════════════════════════════════════════════"
    echo "  全部录制完成！输出目录："
    echo "  ${OUTPUT_DIR}"
    ls -lh "$OUTPUT_DIR"/*.avi 2>/dev/null || echo "  (无 .avi 文件)"
    echo "════════════════════════════════════════════"
fi
