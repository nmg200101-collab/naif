#!/usr/bin/env python3
"""Apply the Smart Screen Control v0.4.0 three-tab UI and safe night-dim upgrade.
Input: verified v0.3.0 project extracted in the working directory.
No network operations; deterministic and validated by GitHub Actions.
"""
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path.cwd()
APP = ROOT / "app/src/main"
NS = "{http://schemas.android.com/apk/res/android}"
ET.register_namespace("android", "http://schemas.android.com/apk/res/android")

def attr(k): return NS + k
def item(tag, ident=None, style=None, **attrs):
    d = {}
    if ident: d[attr("id")] = "@+id/" + ident
    if style: d["style"] = "@style/" + style
    for key, val in attrs.items(): d[attr(key)] = val
    return ET.Element(tag, d)
def text(parent, value, style="SectionHint"):
    e = item("TextView", style=style, text=value)
    parent.append(e)
    return e

layout = APP / "res/layout/activity_main.xml"
tree = ET.parse(layout)
scroll = tree.getroot()
main = scroll[0]
old = list(main)
assert len(old) == 6, "Unsupported baseline for v0.4 upgrade"
header, time_card, tap_card, advanced_button, advanced, footer = old
assert advanced.get(attr("id")) == "@+id/advancedPanel"
assert time_card[0].get(attr("text"), "").startswith("①")
assert tap_card[0].get(attr("text"), "").startswith("②")
header[1].set(attr("text"), "ثلاث أدوات واضحة • خيارات متقدمة عند الحاجة")
time_card[0].set(attr("text"), "وقت تشغيل الشاشة")
tap_card[0].set(attr("text"), "النقر المزدوج")
footer.set(attr("text"), "الإصدار 0.4.0  •  يعمل دون إنترنت  •  التعتيم اختياري")

perms, timeout_card, brightness_card, quick_card, restore_all = list(advanced)
advanced.remove(timeout_card)
advanced.remove(brightness_card)
timeout_card[0].set(attr("text"), "مهلة السكون العادية")
brightness_card[0].set(attr("text"), "السطوع الأساسي للهاتف")
brightness_card[1].set(attr("text"), "لخفض إضاءة الشاشة أكثر من الحد العادي، استخدم التعتيم الإضافي أعلاه.")

for child in old: main.remove(child)
main.append(header)
tabs = item("LinearLayout", orientation="horizontal", layout_width="match_parent",
            layout_height="wrap_content", layout_marginTop="14dp",
            background="@drawable/bg_card", padding="5dp")
for ident, label in [
    ("tabTime", "وقت الشاشة"), ("tabTap", "النقر المزدوج"),
    ("tabDim", "إضاءة الشاشة")
]:
    tabs.append(item("Button", ident=ident, style="DurationButton", text=label,
                     layout_width="0dp", layout_weight="1",
                     layout_height="wrap_content", minHeight="50dp"))
main.append(tabs)

def panel(ident):
    e = item("LinearLayout", ident=ident, layout_width="match_parent",
             layout_height="wrap_content", orientation="vertical",
             visibility="gone")
    main.append(e)
    return e

panel_time = panel("panelTime")
panel_time.append(time_card)
panel_time.append(timeout_card)
panel_tap = panel("panelTap")
panel_tap.append(tap_card)
panel_dim = panel("panelDim")
dim_card = item("LinearLayout", orientation="vertical", layout_width="match_parent",
                layout_height="wrap_content", background="@drawable/bg_card",
                padding="16dp", layout_marginTop="14dp")
text(dim_card, "تعتيم إضافي للراحة ليلًا", "SectionTitle")
text(dim_card, "خفض الضوء الظاهر حتى مع ضبط سطوع الهاتف على أدنى درجة، دون تغيير إعدادات السطوع الأساسية.")
text(dim_card, "التعتيم متوقف", "SecondaryText").set(attr("id"), "@+id/txtDimStatus")
text(dim_card, "قوة التعتيم: 35%", "SecondaryText").set(attr("id"), "@+id/txtDimPercent")
dim_card.append(item("SeekBar", ident="seekDim", layout_width="match_parent",
                     layout_height="wrap_content", max="65", progress="35"))
preset_row = item("LinearLayout", layout_width="match_parent",
                  layout_height="wrap_content", orientation="horizontal")
for ident, label in [
    ("btnDimSoft", "مريح 20%"), ("btnDimNight", "ليلي 40%"),
    ("btnDimDeep", "داكن 60%")
]:
    preset_row.append(item("Button", ident=ident, style="DurationButton",
                           text=label, layout_width="0dp", layout_weight="1"))
dim_card.append(preset_row)
dim_card.append(item("Button", ident="btnDimStart", style="PrimaryButton",
                     text="تفعيل التعتيم الإضافي"))
dim_card.append(item("Button", ident="btnDimStop", style="SecondaryButton",
                     text="إيقاف التعتيم فورًا"))
text(dim_card,
    "مهم: هذه طبقة داكنة فوق التطبيقات، لا تقلل إضاءة الشاشة المادية. "
    "قد يخفيها النظام في شاشات الأمان، ويمكن إيقافها من التطبيق أو إشعار الخدمة. "
    "التعتيم الأقصى 65% للحفاظ على عمل اللمس.")
panel_dim.append(dim_card)
panel_dim.append(brightness_card)
advanced_button.set(attr("text"), "الإعدادات والصلاحيات الأخرى  ▼")
main.append(advanced_button)
main.append(advanced)
main.append(footer)
ET.indent(tree, space="    ")
tree.write(layout, encoding="utf-8", xml_declaration=True)

kt_path = APP / "java/com/smartscreen/control/MainActivity.kt"
s = kt_path.read_text(encoding="utf-8")
assert "private var pendingContinuous = false" in s
s = s.replace("import android.content.Context\n",
              "import android.content.Context\nimport android.content.res.ColorStateList\nimport android.graphics.Color\n")
s = s.replace("    private var pendingContinuous = false\n", """    private var pendingContinuous = false
    private var pendingDim = false
    private lateinit var panelTime: View
    private lateinit var panelTap: View
    private lateinit var panelDim: View
    private lateinit var dimSeek: SeekBar
    private lateinit var dimPercent: TextView
    private lateinit var dimStatus: TextView
""")
needle = "        timeoutSpinner.adapter = ArrayAdapter(this,"
assert needle in s
s = s.replace(needle, """        panelTime = findViewById(R.id.panelTime)
        panelTap = findViewById(R.id.panelTap)
        panelDim = findViewById(R.id.panelDim)
        findViewById<Button>(R.id.tabTime).setOnClickListener { showTab(0) }
        findViewById<Button>(R.id.tabTap).setOnClickListener { showTab(1) }
        findViewById<Button>(R.id.tabDim).setOnClickListener { showTab(2) }
        showTab(0)
        dimSeek = findViewById(R.id.seekDim)
        dimPercent = findViewById(R.id.txtDimPercent)
        dimStatus = findViewById(R.id.txtDimStatus)
        dimSeek.progress = getSharedPreferences("night_dim", MODE_PRIVATE).getInt("level", 35)
        updateDimLabel()
        dimSeek.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
            override fun onProgressChanged(bar: SeekBar, value: Int, fromUser: Boolean) {
                updateDimLabel()
            }
            override fun onStartTrackingTouch(bar: SeekBar) = Unit
            override fun onStopTrackingTouch(bar: SeekBar) {
                if (NightDimService.isRunning) startDim()
            }
        })
        findViewById<Button>(R.id.btnDimSoft).setOnClickListener { presetDim(20) }
        findViewById<Button>(R.id.btnDimNight).setOnClickListener { presetDim(40) }
        findViewById<Button>(R.id.btnDimDeep).setOnClickListener { presetDim(60) }
        findViewById<Button>(R.id.btnDimStart).setOnClickListener { startDim() }
        findViewById<Button>(R.id.btnDimStop).setOnClickListener { stopDim() }
""" + needle)
needle = "        handler.removeCallbacks(refreshRunnable)\n        handler.post(refreshRunnable)"
assert needle in s
s = s.replace(needle, """        if (pendingDim && Settings.canDrawOverlays(this)) {
            pendingDim = false
            startDim()
        }
        handler.removeCallbacks(refreshRunnable)
        handler.post(refreshRunnable)""")
needle = "        currentTimeoutText.text = if (current == Int.MAX_VALUE)"
assert needle in s
s = s.replace(needle, "        refreshDimStatus()\n" + needle)
needle = "    private fun yes(ok: Boolean) = if (ok) "
assert needle in s
methods = """    private fun showTab(which: Int) {
        val panels = listOf(panelTime, panelTap, panelDim)
        val ids = listOf(R.id.tabTime, R.id.tabTap, R.id.tabDim)
        val selected = Color.parseColor("#146C67")
        val unselected = Color.parseColor("#E4F1EF")
        panels.forEachIndexed { index, panel ->
            panel.visibility = if (index == which) View.VISIBLE else View.GONE
            val button = findViewById<Button>(ids[index])
            button.backgroundTintList = ColorStateList.valueOf(
                if (index == which) selected else unselected)
            button.setTextColor(if (index == which) Color.WHITE else Color.parseColor("#234C49"))
        }
        advancedPanel.visibility = View.GONE
        advancedButton.text = "الإعدادات والصلاحيات الأخرى  ▼"
    }

    private fun presetDim(level: Int) {
        dimSeek.progress = level
        startDim()
    }

    private fun updateDimLabel() {
        dimPercent.text = "قوة التعتيم الإضافي: " + dimSeek.progress + "%"
    }

    private fun refreshDimStatus() {
        val active = NightDimService.isRunning
        dimStatus.text = if (active) "● الوضع الليلي الإضافي قيد التشغيل" else
            "○ التعتيم الإضافي متوقف"
        findViewById<Button>(R.id.btnDimStart).text =
            if (active) "تحديث قوة التعتيم" else "تفعيل التعتيم الإضافي"
    }

    private fun startDim() {
        val level = DimRules.normalize(dimSeek.progress)
        if (level == 0) { stopDim(); return }
        if (!Settings.canDrawOverlays(this)) {
            pendingDim = true
            toast("اسمح بالظهور فوق التطبيقات لتفعيل التعتيم الليلي")
            requestOverlayPermission()
            return
        }
        val intent = Intent(this, NightDimService::class.java)
            .setAction(NightDimService.ACTION_START)
            .putExtra(NightDimService.EXTRA_LEVEL, level)
        try {
            if (Build.VERSION.SDK_INT >= 26) startForegroundService(intent)
            else startService(intent)
            getSharedPreferences("night_dim", MODE_PRIVATE).edit()
                .putInt("level", level).apply()
            toast("تم طلب تشغيل التعتيم؛ يمكن إيقافه من إشعار التطبيق أو من هنا")
        } catch (_: Exception) {
            toast("تعذر تشغيل التعتيم؛ راجع صلاحية الظهور فوق التطبيقات")
        }
    }

    private fun stopDim() {
        pendingDim = false
        stopService(Intent(this, NightDimService::class.java))
        getSharedPreferences("night_dim", MODE_PRIVATE).edit()
            .putBoolean("active", false).apply()
        refreshDimStatus()
        toast("تم إيقاف التعتيم")
    }

"""
s = s.replace(needle, methods + needle)
kt_path.write_text(s, encoding="utf-8")

service = r'''package com.smartscreen.control

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.graphics.Color
import android.graphics.PixelFormat
import android.os.Build
import android.os.IBinder
import android.provider.Settings
import android.view.Gravity
import android.view.View
import android.view.WindowManager

/** An optional low-opacity, touch-through screen filter; not hardware backlight control. */
class NightDimService : Service() {
    companion object {
        const val ACTION_START = "com.smartscreen.control.NIGHT_DIM_START"
        const val ACTION_STOP = "com.smartscreen.control.NIGHT_DIM_STOP"
        const val EXTRA_LEVEL = "level"
        @Volatile var isRunning: Boolean = false
            private set
    }
    private val channel = "night_dim_v04"
    private val notificationId = 1728
    private val prefs by lazy { getSharedPreferences("night_dim", MODE_PRIVATE) }
    private var windowManager: WindowManager? = null
    private var screenFilter: View? = null
    private var params: WindowManager.LayoutParams? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        if (intent?.action == ACTION_STOP) {
            stopSelf()
            return START_NOT_STICKY
        }
        if (!Settings.canDrawOverlays(this)) {
            stopSelf()
            return START_NOT_STICKY
        }
        val amount = DimRules.normalize(intent?.getIntExtra(
            EXTRA_LEVEL, prefs.getInt("level", 35)) ?: prefs.getInt("level", 35))
        if (amount == 0) {
            stopSelf()
            return START_NOT_STICKY
        }
        try {
            createNotificationChannel()
            val notice = notification(amount)
            if (Build.VERSION.SDK_INT >= 34) {
                startForeground(notificationId, notice,
                    ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
            } else startForeground(notificationId, notice)
            updateOverlay(amount)
            prefs.edit().putBoolean("active", true).putInt("level", amount).apply()
            isRunning = true
        } catch (_: Exception) {
            stopSelf()
        }
        // Explicit opt-in each time; no persistent restart after the system kills the service.
        return START_NOT_STICKY
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= 26) {
            val manager = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
            manager.createNotificationChannel(NotificationChannel(
                channel, "الراحة البصرية والتعتيم", NotificationManager.IMPORTANCE_LOW))
        }
    }

    private fun notification(level: Int): Notification {
        val open = PendingIntent.getActivity(this, 30,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
        val off = PendingIntent.getService(this, 31,
            Intent(this, NightDimService::class.java).setAction(ACTION_STOP),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
        return Notification.Builder(this, channel)
            .setSmallIcon(R.drawable.ic_app)
            .setContentTitle("التعتيم الليلي مفعّل (" + level + "%)")
            .setContentText("اضغط إيقاف التعتيم للعودة للإضاءة المعتادة")
            .setContentIntent(open)
            .addAction(android.R.drawable.ic_menu_close_clear_cancel, "إيقاف التعتيم", off)
            .setOngoing(true)
            .setCategory(Notification.CATEGORY_SERVICE)
            .build()
    }

    private fun updateOverlay(level: Int) {
        val wm = windowManager ?: (getSystemService(Context.WINDOW_SERVICE)
            as WindowManager).also { windowManager = it }
        val lp = params ?: WindowManager.LayoutParams(
            WindowManager.LayoutParams.MATCH_PARENT,
            WindowManager.LayoutParams.MATCH_PARENT,
            WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY,
            WindowManager.LayoutParams.FLAG_NOT_TOUCHABLE or
                WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE or
                WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN,
            PixelFormat.TRANSLUCENT
        ).apply { gravity = Gravity.FILL }.also { params = it }
        // Android 12+ blocks pass-through touches with overly opaque overlays.
        lp.alpha = DimRules.alpha(level) // <= 0.65, deliberately below 0.8
        if (screenFilter == null) {
            val filter = View(this).apply {
                setBackgroundColor(Color.BLACK)
                importantForAccessibility = View.IMPORTANT_FOR_ACCESSIBILITY_NO
            }
            wm.addView(filter, lp)
            screenFilter = filter
        } else {
            wm.updateViewLayout(screenFilter, lp)
        }
    }

    override fun onDestroy() {
        screenFilter?.let { view ->
            try { windowManager?.removeView(view) } catch (_: Exception) { }
        }
        screenFilter = null
        params = null
        isRunning = false
        prefs.edit().putBoolean("active", false).apply()
        super.onDestroy()
    }
}
'''
service_path = APP / "java/com/smartscreen/control/NightDimService.kt"
service_path.write_text(service, encoding="utf-8")
rules = '''package com.smartscreen.control

/** Conservative transparency limits for pass-through overlay touches on Android 12+. */
object DimRules {
    const val MAX_LEVEL = 65
    fun normalize(value: Int): Int = value.coerceIn(0, MAX_LEVEL)
    fun alpha(value: Int): Float = normalize(value) / 100f
}
'''
(APP / "java/com/smartscreen/control/DimRules.kt").write_text(rules)
testfile = ROOT / "app/src/test/java/com/smartscreen/control/DimRulesTest.kt"
testfile.parent.mkdir(parents=True, exist_ok=True)
testfile.write_text('''package com.smartscreen.control

import org.junit.Assert.assertEquals
import org.junit.Test

class DimRulesTest {
    @Test fun negativeValuesBecomeZero() = assertEquals(0, DimRules.normalize(-20))
    @Test fun maxOpacityIsSafe() = assertEquals(65, DimRules.normalize(100))
    @Test fun noDimAtZero() = assertEquals(0f, DimRules.alpha(0), 0.001f)
    @Test fun nightPreset() = assertEquals(0.40f, DimRules.alpha(40), 0.001f)
    @Test fun maximumAlpha() = assertEquals(0.65f, DimRules.alpha(90), 0.001f)
}
''')

manifest = APP / "AndroidManifest.xml"
mt = ET.parse(manifest)
root = mt.getroot()
application = root.find("application")
assert application is not None
svc = ET.Element("service", {attr("name"): ".NightDimService",
                             attr("exported"): "false",
                             attr("foregroundServiceType"): "specialUse"})
ET.SubElement(svc, "property", {
    attr("name"): "android.app.PROPERTY_SPECIAL_USE_FGS_SUBTYPE",
    attr("value"): "User-enabled dark overlay for reading comfort, with notification stop action"
})
application.append(svc)
ET.indent(mt, space="    ")
mt.write(manifest, encoding="utf-8", xml_declaration=True)

gradle = ROOT / "app/build.gradle.kts"
build = gradle.read_text()
assert 'versionCode = 3' in build and 'versionName = "0.3.0"' in build
gradle.write_text(build.replace("versionCode = 3", "versionCode = 4")
                  .replace('versionName = "0.3.0"', 'versionName = "0.4.0"'))

# Static invariants: no unbound UI ids or duplicate ids; all three panels exist.
import re
ui = layout.read_text()
activity = kt_path.read_text()
ids = re.findall(r"@\+id/(\w+)", ui)
assert len(ids) == len(set(ids)), "Duplicate UI id"
missing = set(re.findall(r"R\.id\.(\w+)", activity)) - set(ids)
assert not missing, "Missing UI ids: " + repr(missing)
assert all(n in ids for n in (
    "tabTime", "tabTap", "tabDim", "panelTime", "panelTap", "panelDim",
    "seekDim", "btnDimStart", "btnDimStop", "btnDimSoft", "btnDimNight", "btnDimDeep"
))
assert len(list(main)) == 7, "Expected header, tabs, 3 panels, advanced and footer"
print("V0.4.0 UI/manifest/Kotlin references validated; added night-dim service + tests.")
