#!/usr/bin/env bash
# 一键运行 cu-004/005/006/008 视觉证据自动截图
# 用法: ./tools/run-evidence-capture.sh [godot-path]
# 示例: ./tools/run-evidence-capture.sh /Applications/Godot_mono.app/Contents/MacOS/Godot

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_PATH="$PROJECT_ROOT/feng-zhi"
OUTPUT_DIR="$PROJECT_ROOT/production/qa/evidence/media"

# Godot 可执行文件路径
GODOT="${1:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"

if [ ! -x "$GODOT" ]; then
    echo "错误: Godot 不在 $GODOT"
    echo "用法: $0 [godot-executable-path]"
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

DEMOS=("cu-004" "cu-005" "cu-006" "cu-008")
SCENES=(
    "res://scenes/vs/demo/battle_demo_cu004.tscn"
    "res://scenes/vs/demo/battle_demo_cu005.tscn"
    "res://scenes/vs/demo/battle_demo_cu006.tscn"
    "res://scenes/vs/demo/battle_demo_cu008.tscn"
)

echo "========================================="
echo "  视觉证据自动截图 — $(date '+%Y-%m-%d %H:%M')"
echo "========================================="
echo "Godot: $GODOT"
echo "项目: $PROJECT_PATH"
echo "输出: $OUTPUT_DIR"
echo ""

PASS_COUNT=0
FAIL_COUNT=0

for i in "${!DEMOS[@]}"; do
    DEMO="${DEMOS[$i]}"
    SCENE="${SCENES[$i]}"

    echo "--- [$DEMO] 启动 ---"

    EVIDENCE_CAPTURE=1 \
    EVIDENCE_OUTPUT_DIR="$OUTPUT_DIR" \
    "$GODOT" \
        --path "$PROJECT_PATH" \
        "$SCENE" \
        --rendering-driver opengl3 \
        --fixed-fps 30 \
        2>&1 | grep -E "\[AutoEvidence\]|ERROR|FAIL" || true

    # 检查是否有截图产出
    SHOT_COUNT=$(find "$OUTPUT_DIR" -name "${DEMO}_*.png" -newer "$0" 2>/dev/null | wc -l | tr -d ' ')
    if [ "$SHOT_COUNT" -gt 0 ]; then
        echo "  ✓ $DEMO: $SHOT_COUNT 张截图"
        ((PASS_COUNT++))
    else
        echo "  ✗ $DEMO: 未产出截图"
        ((FAIL_COUNT++))
    fi
    echo ""
done

echo "========================================="
echo "  结果: $PASS_COUNT 通过, $FAIL_COUNT 失败"
echo "========================================="

if [ "$FAIL_COUNT" -gt 0 ]; then
    echo "存在失败项，请检查 Godot 日志输出。"
    exit 1
fi

echo ""
echo "截图已保存到: $OUTPUT_DIR"
echo "文件列表:"
ls -la "$OUTPUT_DIR"/cu-0*.png 2>/dev/null || echo "(无文件)"
echo ""
echo "下一步: 运行 /story-done 更新 evidence.md 文件。"
