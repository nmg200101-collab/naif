#!/usr/bin/env python3
"""Install background-only network mode with unit tests into Smart Control v0.7."""
from pathlib import Path
import shutil

root = Path.cwd()
code = root / "app/src/main/java/com/smartscreen/control"
test = root / "app/src/test/java/com/smartscreen/control"
test.mkdir(parents=True, exist_ok=True)

for name in ("NetworkRulePolicy.kt", "ForegroundAppWatcher.kt"):
    src = root / "v07" / name
    assert src.exists(), name
    shutil.copyfile(src, code / name)
shutil.copyfile(root / "v07/NetworkRulePolicyTest.kt", test / "NetworkRulePolicyTest.kt")
policy = (code / "NetworkRulePolicy.kt").read_text()
screen = (code / "InternetPanel.kt").read_text()
service = (code / "LocalBlockVpnService.kt").read_text()
watcher = (code / "ForegroundAppWatcher.kt").read_text()
for marker in ("BACKGROUND", "DENY", "ALLOW", "foregroundReliable"):
    assert marker in policy
for marker in ("حظر الخلفية للمحدد", "حظر كامل", "الخلفية فقط",
               "ACTION_USAGE_ACCESS_SETTINGS", "applyWithUsagePermission"):
    assert marker in screen, marker
for marker in ("ForegroundAppWatcher", "packagesForTransport", "isMonitoring",
               "NetworkRulePolicy.isBlocked"):
    assert marker in service, marker
assert "UsageEvents.Event.ACTIVITY_RESUMED" in watcher
assert "snapshot" in watcher
assert "versionName = \"0.7.0\"" in (root / "app/build.gradle.kts").read_text()
print("V0.7 foreground-aware rules and Android usage-consent checks OK")
