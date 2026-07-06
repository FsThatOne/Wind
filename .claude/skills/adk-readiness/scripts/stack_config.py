"""
Shared stack configuration for adk-readiness.

Single source of truth for stack ID → display name / evaluator skill / default module
mapping. All scripts read from this config.

Stack ID convention: <domain>-<variant>
- mobile-android, mobile-ios
- backend-gdp, backend-go
- web, lynx
- generic (fallback)
"""

# Master stack registry.
# Each entry: stack_id -> config dict
#
# Fields per stack:
#   display_name_zh / display_name_en   human-readable labels
#   evaluator_skill                     skill name to invoke via Skill(), null = no evaluator
#   adapter                             which per-stack adaptation logic to use in the runner:
#                                       "mobile" | "gdp" | "web" | "generic" | "fallback"
#   platform                            low-level platform tag (android / ios / go / web / ...)
#   install                             optional list of install guidance.
#       Each option: {"method": "...", "command": "<shell command>", "hint": "<short description>"}
#       method values:
#           "ttadk_plugin_install"   install via ttadk plugin install <url-or-name>
#           "agentbuddy"             install via agentbuddy skill add <registry> --skill <name>
#           "skills_add"             install via skills add <name>
#           "manual"                 generic manual instructions
STACKS = {
    "mobile-android": {
        "display_name_zh": "Mobile / Android",
        "display_name_en": "Mobile / Android",
        "evaluator_skill": "tiktok-mobile-ai-friendliness",
        "adapter": "mobile",
        "platform": "android",
        "install": {
            "options": [
                {
                    "method": "ttadk_plugin_install",
                    "command": "ttadk plugin install https://skills.bytedance.net/skill/skills:skills.byted.org/default/public/tiktok-mobile-ai-friendliness",
                    "hint": "通过 ttadk plugin 方式从技能仓库安装 tiktok-mobile-ai-friendliness",
                },
                {
                    "method": "agentbuddy",
                    "command": "agentbuddy skill add skills.byted.org/default/public --skill tiktok-mobile-ai-friendliness",
                    "hint": "通过 agentbuddy 从公共技能仓库注册 tiktok-mobile-ai-friendliness",
                },
            ],
        },
    },
    "mobile-ios": {
        "display_name_zh": "Mobile / iOS",
        "display_name_en": "Mobile / iOS",
        "evaluator_skill": "tiktok-mobile-ai-friendliness",
        "adapter": "mobile",
        "platform": "ios",
        "install": {
            "options": [
                {
                    "method": "ttadk_plugin_install",
                    "command": "ttadk plugin install https://skills.bytedance.net/skill/skills:skills.byted.org/default/public/tiktok-mobile-ai-friendliness",
                    "hint": "通过 ttadk plugin 方式从技能仓库安装 tiktok-mobile-ai-friendliness",
                },
                {
                    "method": "agentbuddy",
                    "command": "agentbuddy skill add skills.byted.org/default/public --skill tiktok-mobile-ai-friendliness",
                    "hint": "通过 agentbuddy 从公共技能仓库注册 tiktok-mobile-ai-friendliness",
                },
            ],
        },
    },
    "backend-gdp": {
        "display_name_zh": "Backend / GDP",
        "display_name_en": "Backend / GDP",
        "evaluator_skill": "ai-friendly-evaluate-backend-gdp",
        "adapter": "gdp",
        "platform": "go",
        "install": {
            "options": [
                {
                    "method": "ttadk_plugin_install",
                    "command": "ttadk plugin install https://skills.bytedance.net/skill/skills:skills.byted.org/ttls/transaction/ai-friendly-evaluate-backend-gdp",
                    "hint": "通过 ttadk plugin 方式从技能仓库安装 ai-friendly-evaluate-backend-gdp",
                },
                {
                    "method": "agentbuddy",
                    "command": "agentbuddy skill add skills.byted.org/ttls/transaction --skill ai-friendly-evaluate-backend-gdp",
                    "hint": "通过 agentbuddy 从技能仓库注册 ai-friendly-evaluate-backend-gdp",
                },
            ],
        },
    },
    "backend-go": {
        "display_name_zh": "Backend / Go",
        "display_name_en": "Backend / Go",
        "evaluator_skill": "ai-friendly-evaluate-backend-gdp",
        "adapter": "gdp",
        "platform": "go",
        "install": {
            "options": [
                {
                    "method": "ttadk_plugin_install",
                    "command": "ttadk plugin install https://skills.bytedance.net/skill/skills:skills.byted.org/ttls/transaction/ai-friendly-evaluate-backend-gdp",
                    "hint": "通过 ttadk plugin 方式从技能仓库安装 ai-friendly-evaluate-backend-gdp",
                },
                {
                    "method": "agentbuddy",
                    "command": "agentbuddy skill add skills.byted.org/ttls/transaction --skill ai-friendly-evaluate-backend-gdp",
                    "hint": "通过 agentbuddy 从技能仓库注册 ai-friendly-evaluate-backend-gdp",
                },
            ],
        },
    },
    "web": {
        "display_name_zh": "Web 前端",
        "display_name_en": "Web Frontend",
        "evaluator_skill": "ai-friendly-evaluate",
        "adapter": "web",
        "platform": "web",
        "install": {
            "options": [
                {
                    "method": "ttadk_plugin_install",
                    "command": "ttadk plugin install https://skills.bytedance.net/skill/skills:skills.byted.org/default/public/ai-friendly-evaluate",
                    "hint": "通过 ttadk plugin 方式从技能仓库安装 ai-friendly-evaluate",
                },
                {
                    "method": "agentbuddy",
                    "command": "agentbuddy skill add skills.byted.org/default/public --skill ai-friendly-evaluate",
                    "hint": "通过 agentbuddy 从公共技能仓库注册 ai-friendly-evaluate",
                },
            ],
        },
    },
    "lynx": {
        "display_name_zh": "Lynx 跨端",
        "display_name_en": "Lynx Cross-platform",
        "evaluator_skill": None,
        "adapter": "generic",
        "platform": "lynx",
        "install": None,  # no dedicated evaluator yet
    },
    "generic": {
        "display_name_zh": "通用 (未识别)",
        "display_name_en": "Generic",
        "evaluator_skill": None,
        "adapter": "fallback",
        "platform": "generic",
        "install": None,
    },
}


def get(stack_id, key=None, default=None):
    """Get stack config entry or a specific field."""
    entry = STACKS.get(stack_id)
    if entry is None:
        # Unknown stack: build a bare-bones entry using the stack id as name
        entry = {
            "display_name_zh": stack_id,
            "display_name_en": stack_id,
            "evaluator_skill": None,
            "adapter": "generic",
            "platform": stack_id,
            "install": None,
        }
    if key is None:
        return entry
    return entry.get(key, default)


def display_name(stack_id, language="zh"):
    """Return the human-readable display name for a stack."""
    key = "display_name_zh" if language == "zh" else "display_name_en"
    return get(stack_id, key, stack_id)


def evaluator_skill(stack_id):
    """Return the evaluator skill name for a stack, or None."""
    return get(stack_id, "evaluator_skill")


def adapter_type(stack_id):
    """Return the adapter type key used to dispatch per-stack logic."""
    return get(stack_id, "adapter", "generic")


def platform(stack_id):
    """Return the platform identifier (android/ios/go/web/lynx/generic)."""
    return get(stack_id, "platform", "generic")


def install_info(stack_id):
    """Return install guidance for a stack, or None if none configured.

    Returned dict shape: {"options": [ {"method", "command", "hint"}, ... ]}
    """
    return get(stack_id, "install")


def all_stack_ids():
    """Return list of all known stack IDs."""
    return list(STACKS.keys())
