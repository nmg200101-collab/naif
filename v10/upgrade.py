#!/usr/bin/env python3
"""Integrate Smart Control V1.0 opt-in app locker after V0.9 source assembly.

Respect locked baseline: preserve the package identifier, screen, internet,
tethering, phone lock and audio implementations. No elevated device privileges.
"""
from pathlib import Path
import re
import shutil
import xml.etree.ElementTree as ET

root=Path.cwd()
resources=root/"app/src/main/res"
main=root/"app/src/main"
src=main/"java/com/smartscreen/control"
tests=root/"app/src/test/java/com/smartscreen/control"
tests.mkdir(parents=True, exist_ok=True)

for file in (
    "LockCrypto.kt","AppLockStore.kt","PatternDrawView.kt","AppLockPanel.kt",
    "AppLockAccessibilityService.kt","AppBiometricActivity.kt"
):
    shutil.copyfile(root/"v10"/file,src/file)
shutil.copyfile(root/"v10/LockCryptoTest.kt",tests/"LockCryptoTest.kt")

gateway=src/"FeatureGateway.kt"
s=gateway.read_text(encoding="utf-8")
assert "class FeatureGateway(" in s
assert "    fun open(feature:Int) {" in s
s=s.replace("    fun open(feature:Int) {",
"""    private var currentFeature = -1
    private var appLocker: AppLockPanel? = null

    fun refresh() {
        if (currentFeature == 3) appLocker?.refresh()
    }

    fun open(feature:Int) {
        currentFeature = feature
        if (feature == 3) {
            appLocker = AppLockPanel(activity, root)
            appLocker?.render()
            return
        }
        appLocker = null""",1)
gateway.write_text(s,encoding="utf-8")

activity=src/"MainActivity.kt"
s=activity.read_text()
needle="        if (::internetPanel.isInitialized) internetPanel.onResume()"
assert needle in s
s=s.replace(needle,needle+"\n        if (::futureController.isInitialized) futureController.refresh()")
activity.write_text(s)

hub=src/"SmartHomeHub.kt"
s=hub.read_text()
old='Entry("قفل التطبيقات", "الرموز والحماية", "♢", 0xFF42A34A.toInt(), false)'
new='Entry("قفل التطبيقات", "الرموز والحماية", "♢", 0xFF42A34A.toInt(), true)'
assert old in s
hub.write_text(s.replace(old,new))

# Explicit user opt-in via Android accessibility settings.
(resourcess := (resources/"xml")).mkdir(parents=True, exist_ok=True)
(resources/"xml/app_lock_accessibility.xml").write_text("""<?xml version="1.0" encoding="utf-8"?>
<accessibility-service xmlns:android="http://schemas.android.com/apk/res/android"
    android:accessibilityEventTypes="typeWindowStateChanged|typeWindowsChanged"
    android:accessibilityFeedbackType="feedbackGeneric"
    android:notificationTimeout="100"
    android:canRetrieveWindowContent="false"
    android:accessibilityFlags="flagDefault"
    android:isAccessibilityTool="false"
    android:description="@string/app_lock_service_description" />
""", encoding="utf-8")
(resources/"values/app_lock_strings.xml").write_text("""<?xml version="1.0" encoding="utf-8"?>
<resources>
    <string name="app_lock_service_name">التحكم الذكي — قفل التطبيقات</string>
    <string name="app_lock_service_description">بموافقتك، تراقب الخدمة اسم التطبيق المفتوح فقط لتظهر شاشة تحقق عند فتح تطبيق محمي. لا تقرأ محتوى التطبيقات أو الرسائل أو كلمات المرور. يمكنك تعطيلها من إعدادات إمكانية الوصول في أي وقت.</string>
</resources>
""", encoding="utf-8")

manifest=main/"AndroidManifest.xml"
tree=ET.parse(manifest)
r=tree.getroot()
an="{http://schemas.android.com/apk/res/android}"
ET.register_namespace("android", "http://schemas.android.com/apk/res/android")
def a(s): return an+s
if not any(e.get(a("name"))=="android.permission.USE_BIOMETRIC"
    for e in r.findall("uses-permission")):
    r.insert(0,ET.Element("uses-permission",{
        a("name"):"android.permission.USE_BIOMETRIC"
    }))
app=r.find("application")
assert app is not None
assert app.get(a("label"))=="التحكم الذكي"
assert not any(e.get(a("name"))==".AppLockAccessibilityService"
    for e in app.findall("service"))

service=ET.SubElement(app,"service",{
    a("name"):".AppLockAccessibilityService",
    a("label"):"@string/app_lock_service_name",
    a("permission"):"android.permission.BIND_ACCESSIBILITY_SERVICE",
    a("exported"):"true"
})
intent=ET.SubElement(service,"intent-filter")
ET.SubElement(intent,"action",{
    a("name"):"android.accessibilityservice.AccessibilityService"
})
ET.SubElement(service,"meta-data",{
    a("name"):"android.accessibilityservice",
    a("resource"):"@xml/app_lock_accessibility"
})
ET.SubElement(app,"activity",{
    a("name"):".AppBiometricActivity",
    a("exported"):"false",
    a("excludeFromRecents"):"true",
    a("theme"):"@android:style/Theme.Material.Light.NoActionBar"
})
ET.indent(tree,space="    ")
tree.write(manifest,encoding="utf-8",xml_declaration=True)

gradle=root/"app/build.gradle.kts"
s=gradle.read_text()
assert "versionCode = 9" in s
assert 'versionName = "0.9.0"' in s
gradle.write_text(s.replace("versionCode = 9","versionCode = 10")
    .replace('versionName = "0.9.0"','versionName = "1.0.0"'))

# Critical controls and app components all present.
components=[e.get(a("name")) for e in app.findall("service")]
assert ".LocalBlockVpnService" in components
assert ".NightDimService" in components
assert ".AppLockAccessibilityService" in components
assert "com.smartscreen.control" in (root/"app/build.gradle.kts").read_text()
assert "android.permission.BIND_ACCESSIBILITY_SERVICE" in manifest.read_text()
assert 'android:canRetrieveWindowContent="false"' in (
    resources/"xml/app_lock_accessibility.xml").read_text()
assert "LockCrypto.verify" in (src/"AppLockStore.kt").read_text()
assert "AppLockPanel(activity, root)" in gateway.read_text()
assert 'versionName = "1.0.0"' in gradle.read_text()
print("V1.0 app lock integrated, permission opt-in and prior services retained.")
