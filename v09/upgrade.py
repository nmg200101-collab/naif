#!/usr/bin/env python3
"""Apply official V0.9 Smart Control home dashboard without altering legacy controls.

Input: fully assembled V0.8.0 source tree (after v05 + v07 + v08 scripts).
Output: nine-section home, organized native hotspot and safe feature gateways.
"""
from pathlib import Path
import re
import shutil
import xml.etree.ElementTree as ET

root=Path.cwd()
main=root/"app/src/main"
src=main/"java/com/smartscreen/control"
layout=main/"res/layout/activity_main.xml"
manifest=main/"AndroidManifest.xml"
gradle=root/"app/build.gradle.kts"
android="{http://schemas.android.com/apk/res/android}"
ET.register_namespace("android","http://schemas.android.com/apk/res/android")
def a(name): return android+name
def element(tag, **props):
    el=ET.Element(tag)
    for name,val in props.items(): el.set(a(name),val)
    return el

tree=ET.parse(layout)
scroll=tree.getroot()
body=scroll[0]
old=list(body)
assert len(old)==9, f"Unexpected baseline; preserving UI: {len(old)}"
header,tabs,screen,tap,dim,internet,advanced_button,advanced,footer=old
assert internet.get(a("id"))=="@+id/panelInternet"
assert screen.get(a("id"))=="@+id/panelTime"
assert tap.get(a("id"))=="@+id/panelTap"
assert dim.get(a("id"))=="@+id/panelDim"
assert advanced.get(a("id"))=="@+id/advancedPanel"
assert tabs.get(a("orientation"))=="horizontal"
footer.set(a("text"),"التحكم الذكي V0.9.0 • الخصوصية والميزات المحلية • النسخة التجريبية")
header[1].set(a("text"),"إدارة شاملة للهاتف والحماية وتخصيص الأدوات")
tabs.set(a("visibility"),"gone")
advanced_button.set(a("visibility"),"gone")
for child in (screen,tap,dim,internet):
    child.set(a("visibility"),"gone")
adv_btn_id=advanced_button.get(a("id"))
assert adv_btn_id=="@+id/btnAdvanced"
home=element("LinearLayout",id="@+id/dashboardHome",layout_width="match_parent",
    layout_height="wrap_content",orientation="vertical",
    layout_marginTop="12dp",visibility="visible")
btnback=element("Button",id="@+id/btnGoHome",
    layout_width="match_parent",layout_height="wrap_content",
    text="⌂  العودة إلى الأقسام الرئيسية",textAllCaps="false",
    textSize="15sp",textColor="#155548",
    backgroundTint="#D8F6E8",visibility="gone",
    layout_marginTop="8dp")
hotspot=element("LinearLayout",id="@+id/panelHotspot",
    layout_width="match_parent",layout_height="wrap_content",
    orientation="vertical",visibility="gone")
future=element("LinearLayout",id="@+id/panelFeature",
    layout_width="match_parent",layout_height="wrap_content",
    orientation="vertical",visibility="gone")
body.insert(1,home)
body.insert(2,btnback)
pos=list(body).index(internet)
body.insert(pos+1,hotspot)
body.insert(pos+2,future)
ET.indent(tree,space="    ")
tree.write(layout,encoding="utf-8",xml_declaration=True)

for name in ("SmartHomeHub.kt","HotspotPanel.kt","FeatureGateway.kt"):
    f=root/"v09"/name
    assert f.exists(), name
    shutil.copyfile(f,src/name)

kt=src/"MainActivity.kt"
s=kt.read_text()
assert "private lateinit var panelInternet: View" in s
s=s.replace("import android.widget.Button\n",
            "import android.widget.Button\nimport android.widget.LinearLayout\n")
s=s.replace("    private lateinit var panelInternet: View\n",
"""    private lateinit var panelInternet: View
    private lateinit var dashboardHome: View
    private lateinit var panelHotspot: View
    private lateinit var panelFeature: View
    private lateinit var homeNavButton: Button
    private lateinit var mainTabs: View
    private lateinit var hotspotController: HotspotPanel
    private lateinit var futureController: FeatureGateway
    private lateinit var dashboardController: SmartHomeHub
""")
needle="        internetPanel = InternetPanel(this, findViewById(R.id.panelInternet))\n"
assert needle in s
s=s.replace(needle,needle+"""        dashboardHome = findViewById(R.id.dashboardHome)
        panelHotspot = findViewById(R.id.panelHotspot)
        panelFeature = findViewById(R.id.panelFeature)
        mainTabs = findViewById(R.id.tabTime).parent as View
        homeNavButton = findViewById(R.id.btnGoHome)
        hotspotController = HotspotPanel(this, findViewById(R.id.panelHotspot))
        futureController = FeatureGateway(this, findViewById(R.id.panelFeature)) {
            lockNow()
        }
        dashboardController = SmartHomeHub(this, findViewById(R.id.dashboardHome)) {
            selected -> openSection(selected)
        }
        homeNavButton.setOnClickListener { showHomeDashboard() }
        showHomeDashboard()
""")
start=s.index("    private fun showTab(which: Int) {")
assert start>=0
snippet=s[start:start+850]
assert "val panels = listOf(panelTime, panelTap, panelDim, panelInternet)" in snippet
s=s.replace("    private fun showTab(which: Int) {\n",
"""    private fun showTab(which: Int) {
        if (::dashboardHome.isInitialized) {
            dashboardHome.visibility = View.GONE
            panelHotspot.visibility = View.GONE
            panelFeature.visibility = View.GONE
            mainTabs.visibility = View.VISIBLE
            homeNavButton.visibility = View.VISIBLE
            advancedButton.visibility = View.VISIBLE
        }
""")
hook="    private fun presetDim(level: Int) {"
assert hook in s
methods="""    private fun showHomeDashboard() {
        dashboardHome.visibility = View.VISIBLE
        panelHotspot.visibility = View.GONE
        panelFeature.visibility = View.GONE
        listOf(panelTime, panelTap, panelDim, panelInternet).forEach {
            it.visibility = View.GONE
        }
        mainTabs.visibility = View.GONE
        homeNavButton.visibility = View.GONE
        advancedPanel.visibility = View.GONE
        advancedButton.visibility = View.GONE
    }

    private fun openSection(section: Int) {
        when (section) {
            0 -> showTab(0) // display, time and double-tap keep original controls
            1 -> showTab(3) // full network controller with VPN and usage
            else -> {
                dashboardHome.visibility = View.GONE
                listOf(panelTime, panelTap, panelDim, panelInternet).forEach {
                    it.visibility = View.GONE
                }
                mainTabs.visibility = View.GONE
                homeNavButton.visibility = View.VISIBLE
                advancedButton.visibility = View.GONE
                advancedPanel.visibility = View.GONE
                panelHotspot.visibility = if (section == 2) View.VISIBLE else View.GONE
                panelFeature.visibility = if (section == 2) View.GONE else View.VISIBLE
                if (section == 2) hotspotController.redraw()
                else futureController.open(section)
            }
        }
    }

    @Deprecated("Legacy back handling; preserve the original Android back fallback.")
    override fun onBackPressed() {
        if (::dashboardHome.isInitialized && dashboardHome.visibility != View.VISIBLE) {
            showHomeDashboard()
        } else super.onBackPressed()
    }

    override fun onDestroy() {
        if (::hotspotController.isInitialized) hotspotController.cleanup()
        super.onDestroy()
    }

"""
s=s.replace(hook,methods+hook)
kt.write_text(s)

mt=ET.parse(manifest)
r=mt.getroot()
app=r.find("application")
assert app is not None
assert app.get(a("label"))=="التحكم الذكي"
permissions=[
  ("android.permission.CHANGE_WIFI_STATE",{}),
  ("android.permission.NEARBY_WIFI_DEVICES",{}),
  ("android.permission.ACCESS_FINE_LOCATION",{a("maxSdkVersion"):"32"}),
]
for permission,attrs in permissions:
    if not any(el.get(a("name"))==permission for el in r.findall("uses-permission")):
        r.insert(0,ET.Element("uses-permission",{a("name"):permission,**attrs}))
ET.indent(mt,space="    ")
mt.write(manifest,encoding="utf-8",xml_declaration=True)

g=gradle.read_text()
assert "versionCode = 8" in g
assert 'versionName = "0.8.0"' in g
gradle.write_text(g.replace("versionCode = 8","versionCode = 9")
    .replace('versionName = "0.8.0"','versionName = "0.9.0"'))

ids=re.findall(r"@\+id/(\w+)",layout.read_text())
assert len(ids)==len(set(ids)), "duplicate ids"
missing=set(re.findall(r"R\.id\.(\w+)",s))-set(ids)
assert not missing, missing
for marker in ("dashboardHome","btnGoHome","panelHotspot","panelFeature"):
    assert marker in ids
assert "LocalBlockVpnService" in manifest.read_text()
assert "NightDimService" in manifest.read_text()
assert "DoubleTapBubbleService" in manifest.read_text()
assert "versionName = \\"0.9.0\\"" in gradle.read_text()
print("V0.9 interface integrated; nine home sections, hotspot and legacy controls verified.")
