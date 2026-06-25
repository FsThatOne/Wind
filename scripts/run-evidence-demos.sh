#!/bin/bash
# Sprint 7 视觉证据录制 — 4 个 demo 场景快速启动脚本
# 用法: ./scripts/run-evidence-demos.sh [场景编号]
#   无参数 — 逐个运行全部 4 个场景（每个关闭后自动启动下一个）
#   1/2/3/4 — 只运行指定场景

GODOT="/Applications/Godot_mono.app/Contents/MacOS/Godot"
PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)/feng-zhi"

SCENES=(
    "scenes/vs/demo/battle_demo_cu004.tscn"
    "scenes/vs/demo/battle_demo_cu005.tscn"
    "scenes/vs/demo/battle_demo_cu006.tscn"
    "scenes/vs/demo/battle_demo_cu008.tscn"
)

NAMES=(
    "cu-004: 招式面板与预览卡"
    "cu-005: 反制与决胜提示"
    "cu-006: 决胜一击演出"
    "cu-008: 键盘导航与双焦点"
)

TIPS=(
    "↓逐一切换招式看预览卡 | 移到置灰招式看原因 | 移到道具看置灰"
    "找金色反制标签 | [ 降内息看置灰 | ] 恢复 | 看决胜行"
    "聚焦决胜行 → Enter触发演出 → 演出中按键验证屏蔽 → 等结束"
    "↓循环导航 | ↑反向 | 鼠标hover看蓝色+方向键看琥珀双色共存"
)

run_scene() {
    local idx=$1
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  场景 $((idx+1))/4: ${NAMES[$idx]}"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  操作提示: ${TIPS[$idx]}"
    echo "  关闭窗口后自动进入下一个场景"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    "$GODOT" --path "$PROJECT_PATH" "${SCENES[$idx]}" 2>/dev/null
}

if [ -n "$1" ]; then
    idx=$(($1 - 1))
    if [ $idx -ge 0 ] && [ $idx -lt 4 ]; then
        run_scene $idx
    else
        echo "用法: $0 [1-4]"
        exit 1
    fi
else
    echo "╔══════════════════════════════════════════════════╗"
    echo "║  Sprint 7 视觉证据录制 — 共 4 个场景           ║"
    echo "║  请先开启屏幕录制 (Cmd+Shift+5)               ║"
    echo "║  每个场景关闭后自动启动下一个                   ║"
    echo "╚══════════════════════════════════════════════════╝"
    for i in 0 1 2 3; do
        run_scene $i
    done
    echo ""
    echo "✓ 全部 4 个场景录制完成！"
    echo "  请将录屏文件保存到: production/qa/evidence/media/"
fi
