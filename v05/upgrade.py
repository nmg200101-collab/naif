#!/usr/bin/env python3
"""Apply approved light Smart Control visual hierarchy on top of v0.4.0 build.
Keep screen, double tap, extra dim, original package ID unchanged.
The 4th tab is a functional, separately encapsulated InternetPanel.
"""
from pathlib import Path
import re
import shutil
import xml.etree.ElementTree as ET

BASE = Path.cwd()
MAIN = BASE / "app/src/main"
SRC = MAIN / "java/com/smartscreen/control"
AN = "{http://schemas.android.com/apk/res/android}"
ET.register_namespace("android", "http://schemas.android.com/apk/res/android")
def a(key): return AN + key
def node(tag, **kv):
    n = ET.Element(tag)
    for k,v in kv.items(): n.set(a(k),v)
    return n

def replace_resource(path, s):
    path.write_text(s, encoding="utf-8")

layout = MAIN / "res/layout/activity_main.xml"
tree = ET.parse(layout)
scroll = tree.getroot()
body = scroll[0]
old = list(body)
assert len(old) == 8, "Unexpected previous layout. Stop rather than lose controls."
header, tabs, panel_time, panel_tap, panel_dim, more, advanced, footer = old
assert panel_time.get(a("id")) == "@+id/panelTime"
assert panel_dim.get(a("id")) == "@+id/panelDim"
scroll.set(a("background"), "#F1FAF7")
body.set(a("padding"), "12dp")
body.set(a("background"), "#F1FAF7")
header.set(a("background"), "@drawable/bg_clean_header")
header.set(a("padding"), "18dp")
header[0].set(a("text"), "التحكم الذكي")
header[0].set(a("textColor"), "#083839")
header[0].set(a("textSize"), "27sp")
header[0].set(a("gravity"), "center")
header[1].set(a("text"), "إدارة الشاشة والإنترنت والتحكم بالجهاز")
header[1].set(a("textColor"), "#527273")
header[1].set(a("gravity"), "center")
header[1].set(a("textSize"), "14sp")
# Time-only session state is moved from global header to its actual tab.
for item in list(header)[2:]:
    header.remove(item)
    item.set(a("textColor"), "#176C62")
    panel_time.insert(0, item)
header.insert(0, node("TextView",
    layout_width="match_parent",layout_height="wrap_content",gravity="center",
    text="☷", textColor="#12AE81",textSize="30sp"))
tabs.set(a("background"), "@drawable/bg_clean_tabs")
tabs.set(a("padding"), "3dp")
tabs.set(a("layout_marginTop"), "13dp")
for child, name in zip(list(tabs), ("◷\nوقت الشاشة","☝\nالنقر المزدوج","☀\nإضاءة الشاشة")):
    child.set(a("text"), name)
    child.set(a("textSize"), "11sp")
    child.set(a("minHeight"), "64dp")
    child.set(a("padding"), "2dp")
    child.set(a("backgroundTint"), "#FFFFFF")
    child.set(a("textColor"), "#173A3D")
    child.set(a("gravity"), "center")
newtab = node("Button", id="@+id/tabInternet",
    layout_width="0dp",layout_height="wrap_content",
    layout_weight="1",minHeight="64dp",text="◉\nالإنترنت",
    textSize="11sp",textColor="#FFFFFF",textAllCaps="false",
    gravity="center",padding="2dp",backgroundTint="#16AE75")
tabs.append(newtab)

panel_internet = node("LinearLayout",id="@+id/panelInternet",
    layout_width="match_parent",layout_height="wrap_content",
    orientation="vertical",visibility="gone")
body.insert(5, panel_internet)
more.set(a("text"), "⚙  الإعدادات والصلاحيات الأخرى  ▾")
footer.set(a("text"), "التحكم الذكي V0.8.0 • تحكم محلي دون جمع محتوى الاتصالات")
footer.set(a("textColor"), "#6E8A8A")

# All previous tools remain under their respective screens.
ET.indent(tree, space="    ")
tree.write(layout, encoding="utf-8", xml_declaration=True)

replace_resource(MAIN/"res/drawable/bg_clean_header.xml",'''<shape xmlns:android="http://schemas.android.com/apk/res/android">
    <gradient android:startColor="#E8FFF5" android:endColor="#FAFFFF" android:angle="0"/>
    <stroke android:width="1dp" android:color="#CEEAE3"/>
    <corners android:radius="23dp"/>
</shape>''')
replace_resource(MAIN/"res/drawable/bg_clean_tabs.xml",'''<shape xmlns:android="http://schemas.android.com/apk/res/android">
    <solid android:color="#FFFFFF"/>
    <stroke android:width="1dp" android:color="#D0E6E2"/>
    <corners android:radius="19dp"/>
</shape>''')

styles = MAIN/"res/values/styles.xml"
s=styles.read_text()
s=s.replace("#0E5555","#F1FAF7").replace(
    '<item name="android:windowLightStatusBar">false</item>',
    '<item name="android:windowLightStatusBar">true</item>')
styles.write_text(s)
# Keep the clear light look even when the OS uses dark mode.
night = MAIN/"res/values-night/colors.xml"
day = (MAIN/"res/values/colors.xml").read_text()
night.write_text(day.replace("#F4F8F8","#F1FAF7"))
(MAIN/"res/values/colors.xml").write_text(day.replace("#F4F8F8","#F1FAF7"))
(MAIN/"res/values-night/styles.xml").write_text(styles.read_text())

# Correct the launcher artwork to match the approved green control-sliders icon.
replace_resource(MAIN/"res/drawable/ic_app.xml",'''<vector xmlns:android="http://schemas.android.com/apk/res/android"
  android:width="96dp" android:height="96dp"
  android:viewportWidth="96" android:viewportHeight="96">
  <path android:fillColor="#14A979" android:pathData="M0,0h96v96h-96z"/>
  <path android:fillColor="#077E77" android:pathData="M48,0h48v96h-48z"/>
  <path android:strokeColor="#F6FFFE" android:strokeWidth="5"
      android:strokeLineCap="round"
      android:pathData="M17,27h62M17,49h62M17,71h62"/>
  <path android:fillColor="#FFFFFF"
      android:pathData="M31,18a9,9 0,1 0,0 18a9,9 0,1 0,0 -18
                        M62,40a9,9 0,1 0,0 18a9,9 0,1 0,0 -18
                        M42,62a9,9 0,1 0,0 18a9,9 0,1 0,0 -18"/>
</vector>''')

# Integrate a fourth tab/controller without deleting previous wired controls.
kt = SRC / "MainActivity.kt"
s = kt.read_text()
assert "private lateinit var panelDim: View" in s
s=s.replace("    private lateinit var panelDim: View\n",
  "    private lateinit var panelDim: View\n"+
  "    private lateinit var panelInternet: View\n"+
  "    private lateinit var internetPanel: InternetPanel\n")
s=s.replace("        panelDim = findViewById(R.id.panelDim)\n",
  "        panelDim = findViewById(R.id.panelDim)\n"+
  "        panelInternet = findViewById(R.id.panelInternet)\n")
s=s.replace("        findViewById<Button>(R.id.tabDim).setOnClickListener { showTab(2) }\n",
  "        findViewById<Button>(R.id.tabDim).setOnClickListener { showTab(2) }\n"+
  "        findViewById<Button>(R.id.tabInternet).setOnClickListener { showTab(3) }\n")
assert "        showTab(0)\n" in s
s=s.replace("        showTab(0)\n",
    "        showTab(3)\n        internetPanel = InternetPanel(this, findViewById(R.id.panelInternet))\n",1)
s=s.replace("val panels = listOf(panelTime, panelTap, panelDim)",
  "val panels = listOf(panelTime, panelTap, panelDim, panelInternet)")
s=s.replace("val ids = listOf(R.id.tabTime, R.id.tabTap, R.id.tabDim)",
  "val ids = listOf(R.id.tabTime, R.id.tabTap, R.id.tabDim, R.id.tabInternet)")
s=s.replace('Color.parseColor("#146C67")','Color.parseColor("#11AC78")')
s=s.replace('Color.parseColor("#E4F1EF")','Color.parseColor("#FFFFFF")')
s=s.replace('Color.parseColor("#234C49")','Color.parseColor("#173A3D")')
assert "        control.reconcileSession()\n" in s
s=s.replace("        control.reconcileSession()\n",
  "        control.reconcileSession()\n        if (::internetPanel.isInitialized) internetPanel.onResume()\n",1)
needle="    private fun yes(ok: Boolean) = if (ok)"
assert needle in s
s=s.replace(needle,'''    @Deprecated("Use Activity Result APIs in a future migration")
    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        super.onActivityResult(requestCode, resultCode, data)
        if (requestCode == 508 && resultCode == Activity.RESULT_OK &&
            ::internetPanel.isInitialized) internetPanel.onVpnGranted()
        if (requestCode == 508 && resultCode != Activity.RESULT_OK)
            toast("لم يبدأ الحظر: لم تُمنح صلاحية VPN")
    }

'''+needle)
kt.write_text(s)

# Manifest permissions: Android approves VPN through a system consent screen.
manifest=MAIN/"AndroidManifest.xml"
mt=ET.parse(manifest)
mr=mt.getroot()
mr.find("application").set(a("label"), "التحكم الذكي")
for permission in (
    "android.permission.INTERNET",
    "android.permission.ACCESS_NETWORK_STATE",
    "android.permission.QUERY_ALL_PACKAGES",
    "android.permission.PACKAGE_USAGE_STATS",
):
    if not any(el.get(a("name"))==permission for el in mr.findall("uses-permission")):
        mr.insert(0,ET.Element("uses-permission",{a("name"):permission}))

app=mr.find("application")
service=ET.Element("service",{
    a("name"):".LocalBlockVpnService",
    a("exported"):"true",
    a("permission"):"android.permission.BIND_VPN_SERVICE",
    a("foregroundServiceType"):"specialUse"
})
flt=ET.SubElement(service,"intent-filter")
ET.SubElement(flt,"action",{a("name"):"android.net.VpnService"})
ET.SubElement(service,"property",{
    a("name"):"android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE",
    a("value"):"User-initiated local per-app network blocker, disconnect with foreground notification"
})
app.append(service)
ET.indent(mt,space="    ")
mt.write(manifest,encoding="utf-8",xml_declaration=True)

for name in ("InternetPanel.kt","LocalBlockVpnService.kt"):
    shutil.copyfile(BASE/"v05"/name, SRC/name)

gradle=BASE/"app/build.gradle.kts"
g=gradle.read_text()
assert 'versionCode = 4' in g
gradle.write_text(g.replace("versionCode = 4","versionCode = 8")
  .replace('versionName = "0.4.0"','versionName = "0.8.0"'))

# Static contract checks.
ET.parse(layout)
ET.parse(manifest)
ids = re.findall(r'@\+id/(\w+)',layout.read_text())
assert len(ids)==len(set(ids)), "Duplicate view ID"
missing=set(re.findall(r'R\.id\.(\w+)',kt.read_text()))-set(ids)
assert not missing, f"Unresolved ids: {missing}"
assert "android.permission.BIND_VPN_SERVICE" in manifest.read_text()
assert "التحكم الذكي" in manifest.read_text()
assert "panelInternet" in ids and "tabInternet" in ids
print("V0.8 UI merged, old screen functions retained, VPN blocker and usage UI connected.")
