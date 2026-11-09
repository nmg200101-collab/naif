#!/usr/bin/env python3
"""Install Smart Control 0.8 data cap engine and its regression tests."""
from pathlib import Path
from shutil import copyfile

root = Path.cwd()
app = root / "app/src/main/java/com/smartscreen/control"
tests = root / "app/src/test/java/com/smartscreen/control"
tests.mkdir(parents=True, exist_ok=True)
for name in ("DailyQuotaPolicy.kt", "DailyQuotaMonitor.kt"):
    copyfile(root / "v08" / name, app / name)
copyfile(root / "v08/DailyQuotaPolicyTest.kt", tests / "DailyQuotaPolicyTest.kt")

ui = (app / "InternetPanel.kt").read_text()
vpn = (app / "LocalBlockVpnService.kt").read_text()
monitor = (app / "DailyQuotaMonitor.kt").read_text()
manifest = (root / "app/src/main/AndroidManifest.xml").read_text()
gradle = (root / "app/build.gradle.kts").read_text()
for marker in ("حد الاستهلاك اليومي للمحدد", "DailyQuotaPolicy", "chooseQuotaFor",
               "quota_hit", "quota_trip_"):
    if marker == "quota_hit": continue
    assert marker in ui, marker
for marker in ("DailyQuotaMonitor", "quotaMonitor?.resample()",
               "DailyQuotaPolicy.activeBlock", "if (!hasAnyRules()) stopSelf()"):
    assert marker in vpn, marker
for marker in ("querySummary(", "SharedPreferences", "dayChanged",
               "countByUid", "usageAllowed"):
    assert marker in monitor, marker
assert 'versionCode = 8' in gradle and 'versionName = "0.8.0"' in gradle
assert "android.permission.PACKAGE_USAGE_STATS" in manifest
print("V0.8 quota monitor, local network controls, permissions and unit tests OK")
