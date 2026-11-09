package com.smartscreen.control

import android.app.Activity
import android.app.AppOpsManager
import android.app.usage.NetworkStats
import android.app.usage.NetworkStatsManager
import android.content.Context
import android.content.Intent
import android.content.pm.ApplicationInfo
import android.content.pm.PackageManager
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.net.ConnectivityManager
import android.net.VpnService
import android.os.Build
import android.os.Process
import android.provider.Settings
import android.view.Gravity
import android.view.View
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.ScrollView
import android.widget.Spinner
import android.widget.ArrayAdapter
import android.widget.TextView
import android.widget.Toast
import android.widget.Switch
import android.widget.CheckBox
import android.widget.HorizontalScrollView
import android.net.Uri
import java.util.Calendar
import java.util.Locale
import kotlin.concurrent.thread

/**
 * The network tab is built as native Android views to preserve the exact hierarchy of the
 * approved light UI: access tabs, network actions, apps, VPN, data usage, advanced.
 *
 * A VPN blackhole only receives traffic of blocked packages. No payload is logged.
 * "VPN improvement" opens Android's real VPN settings; no fictitious speed boost.
 */
class InternetPanel(
    private val activity: Activity,
    private val root: LinearLayout
) {
    private val prefs = activity.getSharedPreferences("network_rules_v05", Context.MODE_PRIVATE)
    private val white = Color.WHITE
    private val ink = Color.rgb(9, 43, 48)
    private val muted = Color.rgb(87, 111, 116)
    private val line = Color.rgb(205, 228, 228)
    private val green = Color.rgb(19, 173, 103)
    private val red = Color.rgb(219, 76, 87)
    private val blue = Color.rgb(24, 132, 223)
    private val pale = Color.rgb(245, 251, 249)
    private val apps by lazy { installedApps() }
    private lateinit var section: LinearLayout
    private lateinit var accessStatus: TextView
    private lateinit var appsColumn: LinearLayout
    private lateinit var scope: Spinner
    private lateinit var vpnStatus: TextView
    private lateinit var wifiStat: TextView
    private lateinit var mobileStat: TextView
    private lateinit var rulesDescription: TextView
    private var profile = 0 // 0 Wi-Fi, 1 cellular, 2 usage, 3 VPN
    private var showAll = false
    private val selectedPackages = linkedSetOf<String>()
    private lateinit var selectionInfo: TextView
    private lateinit var usageAppsColumn: LinearLayout
    private var usagePeriod = 2 // 0 today, 1 last 7 days, 2 month
    private var statsGeneration = 0
    private data class AppUsage(val name: String, val wifi: Long, val mobile: Long)
    private var usageRows = emptyList<AppUsage>()
    private var showAllUsage = false
    private lateinit var usageInfo: TextView
    private lateinit var usageAccess: Button
    private lateinit var usageShowAll: Button

    private fun dp(value: Int): Int = (value * activity.resources.displayMetrics.density + 0.5f).toInt()
    private fun bg(color: Int, r: Int = 18, edge: Int = line): GradientDrawable =
        GradientDrawable().apply {
            setColor(color)
            cornerRadius = dp(r).toFloat()
            setStroke(dp(1), edge)
        }
    private fun linear(horizontal: Boolean = false) = LinearLayout(activity).apply {
        orientation = if (horizontal) LinearLayout.HORIZONTAL else LinearLayout.VERTICAL
        layoutDirection = View.LAYOUT_DIRECTION_RTL
    }
    private fun styleText(value: String, size: Float = 14f, color: Int = ink,
                          bold: Boolean = false): TextView = TextView(activity).apply {
        text = value
        textSize = size
        setTextColor(color)
        gravity = Gravity.CENTER_VERTICAL or Gravity.RIGHT
        if (bold) setTypeface(null, Typeface.BOLD)
        setPadding(0, dp(3), 0, dp(3))
    }
    private fun card(): LinearLayout = linear().apply {
        background = bg(white, 20)
        setPadding(dp(15), dp(15), dp(15), dp(15))
        val lp = LinearLayout.LayoutParams(-1, -2)
        lp.topMargin = dp(12)
        layoutParams = lp
        elevation = dp(2).toFloat()
    }
    private fun action(name: String, color: Int, whiteText: Boolean = true): Button =
        Button(activity).apply {
            text = name
            textSize = 14f
            isAllCaps = false
            setTypeface(null, Typeface.BOLD)
            setTextColor(if (whiteText) white else ink)
            background = bg(color, 14, color)
            backgroundTintList = null
            minHeight = dp(48)
            minimumHeight = dp(48)
        }
    private fun paddedRow(left: View, right: View): LinearLayout = linear(true).apply {
        addView(left, LinearLayout.LayoutParams(0, -2, 1f))
        addView(right, LinearLayout.LayoutParams(0, -2, 1f))
    }
    private fun margin(target: View, top: Int) {
        val lp = (target.layoutParams ?: LinearLayout.LayoutParams(-1, -2))
        if (lp is LinearLayout.LayoutParams) {
            lp.topMargin = dp(top)
            target.layoutParams = lp
        }
    }

    init { showProfile(0) }

    private fun showProfile(next: Int) {
        profile = next
        root.removeAllViews()
        val top = card()
        top.addView(styleText("إدارة الإنترنت", 21f, ink, true))
        top.addView(styleText("تحكم واضح في اتصال التطبيقات والبيانات", 13f, muted))
        root.addView(top)

        val scroll = HorizontalScrollView(activity).apply {
            isHorizontalScrollBarEnabled = false
            fillViewport = true
        }
        val options = linear(true)
        val labels = listOf("الواي فاي", "بيانات الهاتف", "استخدام البيانات", "VPN التسريع")
        labels.forEachIndexed { index, name ->
            val b = action(name, if (index == profile) green else white, index == profile)
            b.textSize = 12f
            if (index != profile) b.setTextColor(ink)
            val lp = LinearLayout.LayoutParams(dp(112), dp(55))
            lp.setMargins(dp(3), dp(4), dp(3), dp(2))
            options.addView(b, lp)
            b.setOnClickListener { showProfile(index) }
        }
        scroll.addView(options)
        root.addView(scroll)

        when (profile) {
            0, 1 -> {
                selectedPackages.clear()
                buildAccess()
                buildSummary()
                buildAdvanced()
            }
            2 -> buildUsage()
            3 -> buildVpnCard()
        }
        margin(top, 8)
    }

    private fun key() = if (profile == 1) "mobile" else "wifi"
    private fun keyFor(pkg: String, p: String = key()) = "rule_" + p + "_" + pkg
    private fun isBlocked(pkg: String, p: String = key()): Boolean {
        val override = prefs.getString(keyFor(pkg, p), "") ?: ""
        return when (override) {
            "deny" -> true
            "allow" -> false
            else -> prefs.getBoolean("all_" + p, false)
        }
    }
    private fun installedApps(): List<ApplicationInfo> {
        @Suppress("DEPRECATION")
        val list = activity.packageManager.getInstalledApplications(0)
        return list.filter {
            it.packageName != activity.packageName &&
                ((it.flags and ApplicationInfo.FLAG_SYSTEM) == 0 ||
                 activity.packageManager.getLaunchIntentForPackage(it.packageName) != null)
        }.sortedBy { app ->
            app.loadLabel(activity.packageManager).toString().lowercase(Locale.getDefault())
        }.take(320)
    }

    private fun buildAccess() {
        val hero = card()
        val title = if (profile == 0) "إدارة الواي فاي" else "إدارة بيانات الهاتف"
        val subtitle = if (profile == 0) "اتصال التطبيقات عبر الواي فاي" else
            "اتصال التطبيقات عبر بيانات الهاتف"
        hero.addView(styleText("◉  " + title, 21f, ink, true))
        hero.addView(styleText(subtitle, 14f, muted))
        hero.addView(styleText("◠  )))", 36f, blue, true).apply {
            gravity = Gravity.CENTER
            setPadding(0, dp(2), 0, dp(2))
        })
        accessStatus = styleText("", 12f, muted)
        hero.addView(accessStatus)

        val buttons = linear(true)
        val deny = action("⊘  حظر الإنترنت", red)
        val allow = action("◉  فتح الإنترنت", green)
        val lp = LinearLayout.LayoutParams(0, dp(53), 1f)
        lp.setMargins(dp(3), dp(9), dp(3), dp(3))
        buttons.addView(deny, lp)
        val lp2 = LinearLayout.LayoutParams(0, dp(53), 1f)
        lp2.setMargins(dp(3), dp(9), dp(3), dp(3))
        buttons.addView(allow, lp2)
        hero.addView(buttons)

        val scopeRow = linear(true)
        val caption = styleText("تطبيق على:", 13f, ink, true)
        scopeRow.addView(caption, LinearLayout.LayoutParams(-2, dp(50)))
        scope = Spinner(activity)
        val choices = mutableListOf("جميع التطبيقات")
        choices.addAll(apps.map { it.loadLabel(activity.packageManager).toString() })
        scope.adapter = ArrayAdapter(activity, android.R.layout.simple_spinner_dropdown_item, choices)
        scopeRow.addView(scope, LinearLayout.LayoutParams(0, dp(50), 1f))
        hero.addView(scopeRow)
        root.addView(hero)

        deny.setOnClickListener { changeRule(true) }
        allow.setOnClickListener { changeRule(false) }

        val appCard = card()
        appCard.addView(styleText("▣  إدارة التطبيقات", 19f, ink, true))
        appCard.addView(styleText("السماح أو الحظر لكل تطبيق بحسب الشبكة المختارة", 12f, muted))
        selectionInfo = styleText("التطبيقات المحددة: 0", 12f, muted)
        appCard.addView(selectionInfo)
        val pickRow = linear(true)
        val allButton = action("تحديد الكل", pale, false)
        val noneButton = action("إلغاء التحديد", pale, false)
        pickRow.addView(allButton, LinearLayout.LayoutParams(0, dp(48), 1f))
        pickRow.addView(noneButton, LinearLayout.LayoutParams(0, dp(48), 1f))
        appCard.addView(pickRow)
        val applyRow = linear(true)
        val blockSelected = action("حظر المحدد", red)
        val allowSelected = action("فتح المحدد", green)
        applyRow.addView(blockSelected, LinearLayout.LayoutParams(0, dp(48), 1f))
        applyRow.addView(allowSelected, LinearLayout.LayoutParams(0, dp(48), 1f))
        appCard.addView(applyRow)
        allButton.setOnClickListener {
            selectedPackages.clear()
            selectedPackages.addAll(apps.map { it.packageName })
            renderApps()
        }
        noneButton.setOnClickListener {
            selectedPackages.clear()
            renderApps()
        }
        blockSelected.setOnClickListener { applySelectedRules(true) }
        allowSelected.setOnClickListener { applySelectedRules(false) }
        val showButton = action("عرض جميع التطبيقات", pale, false)
        appCard.addView(showButton)
        appsColumn = linear()
        appCard.addView(appsColumn)
        root.addView(appCard)
        showButton.setOnClickListener {
            showAll = !showAll
            showButton.text = if (showAll) "عرض أقل" else "عرض جميع التطبيقات"
            renderApps()
        }
        renderApps()
        updateAccessStatus()
    }

    private fun renderApps() {
        appsColumn.removeAllViews()
        if (apps.isEmpty()) {
            appsColumn.addView(styleText("لا توجد تطبيقات متاحة للعرض على هذا الجهاز", 13f, muted))
            return
        }
        val visible = if (showAll) apps else apps.take(3)
        visible.forEach { app ->
            val row = linear(true).apply {
                background = bg(pale, 12)
                setPadding(dp(10), dp(6), dp(10), dp(6))
            }
            val name = app.loadLabel(activity.packageManager).toString()
            val check = CheckBox(activity).apply {
                isChecked = selectedPackages.contains(app.packageName)
                contentDescription = "تحديد " + name + " للإجراء الجماعي"
                setOnCheckedChangeListener { _, selected ->
                    if (selected) selectedPackages.add(app.packageName)
                    else selectedPackages.remove(app.packageName)
                    updateSelectionInfo()
                }
            }
            row.addView(check)
            row.addView(styleText(name, 14f, ink, true),
                LinearLayout.LayoutParams(0, dp(48), 1f))
            val toggle = Switch(activity)
            toggle.isChecked = !isBlocked(app.packageName)
            toggle.contentDescription = "السماح بالإنترنت لـ " + name
            row.addView(toggle)
            appsColumn.addView(row)
            margin(row, 6)
            toggle.setOnCheckedChangeListener { _, granted ->
                prefs.edit().putString(keyFor(app.packageName),
                    if (granted) "allow" else "deny").apply()
                applyBlockerIfReady()
            }
        }
        updateSelectionInfo()
    }

    private fun updateSelectionInfo() {
        if (::selectionInfo.isInitialized) {
            selectionInfo.text = "المحدد: " + selectedPackages.size + " من " + apps.size +
                " • اختر ثم اضغط حظر المحدد أو فتح المحدد"
        }
    }

    private fun applySelectedRules(block: Boolean) {
        if (selectedPackages.isEmpty()) {
            Toast.makeText(activity, "حدد تطبيقًا واحدًا على الأقل أولًا",
                Toast.LENGTH_SHORT).show()
            return
        }
        val edit = prefs.edit()
        val target = key()
        selectedPackages.forEach { pkg ->
            edit.putString(keyFor(pkg, target), if (block) "deny" else "allow")
        }
        edit.apply()
        Toast.makeText(activity, if (block) "تم حفظ حظر " + selectedPackages.size + " تطبيق"
            else "تم حفظ السماح لـ " + selectedPackages.size + " تطبيق",
            Toast.LENGTH_LONG).show()
        selectedPackages.clear()
        renderApps()
        applyBlockerIfReady()
    }

    private fun changeRule(block: Boolean) {
        val index = scope.selectedItemPosition
        if (index <= 0) {
            val edit = prefs.edit().putBoolean("all_" + key(), block)
            // A bulk action resets old per-app exceptions so the user has a predictable result.
            prefs.all.keys.filter { it.startsWith("rule_" + key() + "_") }
                .forEach { edit.remove(it) }
            edit.apply()
        } else {
            val app = apps.getOrNull(index - 1) ?: return
            prefs.edit().putString(keyFor(app.packageName),
                if (block) "deny" else "allow").apply()
        }
        renderApps()
        applyBlockerIfReady()
    }

    private fun applyBlockerIfReady() {
        updateAccessStatus()
        val prepare = VpnService.prepare(activity)
        if (prepare != null) {
            Toast.makeText(activity,
                "حُفظت القواعد. وافق على إذن VPN لتطبيق الحظر فعليًا.", Toast.LENGTH_LONG).show()
            activity.startActivityForResult(prepare, 508)
            return
        }
        applyBlocker()
    }

    fun onVpnGranted() = applyBlocker()
    fun onResume() {
        when (profile) {
            0, 1 -> if (::accessStatus.isInitialized) updateAccessStatus()
            2 -> if (::usageAppsColumn.isInitialized && usageAllowed()) loadUsage()
        }
    }

    private fun applyBlocker() {
        val intent = Intent(activity, LocalBlockVpnService::class.java)
            .setAction(LocalBlockVpnService.ACTION_APPLY)
        try {
            if (Build.VERSION.SDK_INT >= 26) activity.startForegroundService(intent)
            else activity.startService(intent)
            Toast.makeText(activity,
                "تم إرسال قواعد الحظر إلى VPN المحلي. تحقق من حالة الاتصال.",
                Toast.LENGTH_LONG).show()
        } catch (e: Exception) {
            Toast.makeText(activity,
                "تعذر تفعيل VPN. تحقق من إعدادات الهاتف.", Toast.LENGTH_LONG).show()
        }
        updateAccessStatus()
    }

    private fun updateAccessStatus() {
        val count = apps.count { isBlocked(it.packageName) }
        val state = if (LocalBlockVpnService.isRunning)
            "VPN المحلي يعمل • التطبيقات المحظورة في هذه الشبكة: " + count
        else if (count > 0)
            "هناك " + count + " قاعدة حظر محفوظة؛ VPN غير نشط"
        else "الإنترنت مسموح للتطبيقات"
        accessStatus.text = state
    }

    private fun buildVpnCard() {
        val vpn = card()
        vpn.addView(styleText("♢  VPN لتحسين الاتصال", 19f, ink, true))
        vpnStatus = styleText("VPN خارجي غير متصل من داخل التطبيق", 12f, red)
        vpn.addView(vpnStatus)
        vpn.addView(styleText("قسم مستقل لإدارة VPN واختبار اتصالك. لا يوجد خادم VPN للتسريع "
            + "داخل هذا التطبيق، وقد يزيد VPN خارجي زمن الاستجابة أو يخفض السرعة.", 12f, muted))
        vpn.addView(styleText("ملاحظة: يستخدم حظر التطبيقات VPN محليًا. لا يمكن تشغيل "
            + "VPN منفصل للتسريع بالتزامن معه.", 12f, muted))
        val b = action("⏻  تشغيل VPN", blue)
        vpn.addView(b)
        b.setOnClickListener {
            if (LocalBlockVpnService.isRunning) {
                Toast.makeText(activity,
                    "يعمل VPN للحظر حاليًا. تشغيل VPN آخر سيوقف الحظر وقد يغير الإنترنت.",
                    Toast.LENGTH_LONG).show()
            }
            try { activity.startActivity(Intent(Settings.ACTION_VPN_SETTINGS)) }
            catch (_: Exception) {
                Toast.makeText(activity, "افتح إعدادات VPN يدويًا من إعدادات الشبكة",
                    Toast.LENGTH_LONG).show()
            }
        }
        val speedCheck = action("اختبار سرعة الإنترنت من المتصفح", pale, false)
        vpn.addView(speedCheck)
        speedCheck.setOnClickListener {
            try {
                activity.startActivity(Intent(Intent.ACTION_VIEW, Uri.parse("https://fast.com/")))
            } catch (_: Exception) {
                Toast.makeText(activity, "تعذر فتح المتصفح لاختبار السرعة",
                    Toast.LENGTH_SHORT).show()
            }
        }
        val current = styleText(if (LocalBlockVpnService.isRunning)
            "VPN الحظر المحلي نشط الآن" else "VPN الحظر المحلي غير نشط", 13f, muted)
        vpn.addView(current)
        val stop = action("إيقاف VPN المحلي للحظر", pale, false)
        vpn.addView(stop)
        stop.setOnClickListener {
            activity.stopService(Intent(activity, LocalBlockVpnService::class.java))
            current.text = "تم طلب إيقاف VPN الحظر المحلي"
        }
        root.addView(vpn)
        val limits = card()
        limits.addView(styleText("حدود سرعة كل تطبيق", 18f, ink, true))
        limits.addView(styleText("غير متاحة للتفعيل بعد: محرك الحظر الحالي يسقط "
            + "حركة التطبيقات المحظورة، ولا يمرر حركة التطبيقات المسموحة. "
            + "تحديد سرعة تحميل ورفع حقيقية يتطلب محرك VPN لتمرير البيانات "
            + "وتطبيق حدود النطاق الترددي دون قطع الاتصال.", 12f, muted))
        root.addView(limits)
    }

    private fun buildUsage() {
        showAllUsage = false
        usageRows = emptyList()
        val usage = card()
        usage.addView(styleText("▥  استخدام البيانات", 20f, ink, true))
        usage.addView(styleText("إحصاءات حقيقية، لا قيم تقديرية، بعد منح إذن الاستخدام", 12f, muted))
        val periods = linear(true)
        listOf("اليوم", "آخر 7 أيام", "هذا الشهر").forEachIndexed { index, label ->
            val b = action(label, if (usagePeriod == index) green else pale,
                usagePeriod == index)
            b.textSize = 12f
            periods.addView(b, LinearLayout.LayoutParams(0, dp(48), 1f))
            b.setOnClickListener {
                usagePeriod = index
                buildCurrentUsage()
            }
        }
        usage.addView(periods)
        val row = linear(true)
        val w = linear().apply {
            background = bg(pale)
            setPadding(dp(10), dp(10), dp(10), dp(10))
        }
        wifiStat = styleText("الواي فاي\n—", 15f, ink, true)
        w.addView(wifiStat)
        val m = linear().apply {
            background = bg(pale)
            setPadding(dp(10), dp(10), dp(10), dp(10))
        }
        mobileStat = styleText("بيانات الهاتف\n—", 15f, ink, true)
        m.addView(mobileStat)
        row.addView(w, LinearLayout.LayoutParams(0, -2, 1f))
        row.addView(m, LinearLayout.LayoutParams(0, -2, 1f))
        usage.addView(row)
        val refresh = action("تحديث إحصاءات الاستهلاك", pale, false)
        usage.addView(refresh)
        refresh.setOnClickListener { loadUsage() }
        usageAccess = action("منح إذن بيانات الاستخدام", blue)
        usage.addView(usageAccess)
        usageAccess.setOnClickListener {
            try { activity.startActivity(Intent(Settings.ACTION_USAGE_ACCESS_SETTINGS)) }
            catch (_: Exception) {
                Toast.makeText(activity, "افتح إعدادات الوصول إلى الاستخدام يدويًا",
                    Toast.LENGTH_LONG).show()
            }
        }
        root.addView(usage)

        val perApp = card()
        perApp.addView(styleText("▤  استهلاك كل تطبيق", 19f, ink, true))
        perApp.addView(styleText("ترتيب التطبيقات الأعلى استهلاكًا مع فصل الواي فاي عن بيانات الهاتف",
            12f, muted))
        usageInfo = styleText("جارٍ إعداد التقرير…", 13f, muted)
        perApp.addView(usageInfo)
        usageAppsColumn = linear()
        perApp.addView(usageAppsColumn)
        usageShowAll = action("عرض كل التطبيقات المستهلكة", pale, false)
        perApp.addView(usageShowAll)
        usageShowAll.visibility = View.GONE
        usageShowAll.setOnClickListener {
            showAllUsage = !showAllUsage
            renderUsageRows()
        }
        root.addView(perApp)

        val limits = card()
        limits.addView(styleText("حدود سرعة الإنترنت لكل تطبيق", 18f, ink, true))
        limits.addView(styleText(
            "غير مفعلة في هذا الإصدار. التحكم الفعلي في سرعة التحميل والرفع "
              + "يحتاج محرك VPN يمرر الاتصالات ويطبّق معدل البايتات، "
              + "ولا يمكن لمحرك حظر التطبيقات الحالي القيام بذلك.",
            12f, muted))
        root.addView(limits)
        loadUsage()
    }

    private fun buildCurrentUsage() {
        // Rebuild only the usage page, preserving the chosen time period.
        if (profile == 2) showProfile(2)
    }

    private fun renderUsageRows() {
        if (!::usageAppsColumn.isInitialized) return
        usageAppsColumn.removeAllViews()
        if (usageRows.isEmpty()) {
            usageAppsColumn.addView(styleText("لم تُسجّل بيانات تطبيقات متاحة لهذه الفترة",
                12f, muted))
            usageShowAll.visibility = View.GONE
            return
        }
        val rows = if (showAllUsage) usageRows else usageRows.take(15)
        rows.forEachIndexed { index, row ->
            val item = linear().apply {
                background = bg(pale, 12)
                setPadding(dp(10), dp(7), dp(10), dp(7))
            }
            item.addView(styleText((index + 1).toString() + ". " + row.name,
                14f, ink, true))
            item.addView(styleText("واي فاي: " + humanBytes(row.wifi) +
                "     بيانات الهاتف: " + humanBytes(row.mobile), 12f, muted))
            usageAppsColumn.addView(item)
            margin(item, 5)
        }
        usageShowAll.visibility =
            if (usageRows.size > 15) View.VISIBLE else View.GONE
        usageShowAll.text = if (showAllUsage) "عرض أول 15 تطبيقًا" else
            "عرض كل التطبيقات المستهلكة (" + usageRows.size + ")"
    }

    private fun buildSummary() {
        val card = card()
        card.addView(styleText("▥  استخدام البيانات", 18f, ink, true))
        card.addView(styleText("افتح تبويب «استخدام البيانات» للاطلاع على الإحصاءات", 12f, muted))
        val b = action("عرض الإحصاءات", pale, false)
        card.addView(b)
        b.setOnClickListener { showProfile(2) }
        root.addView(card)
    }

    private fun usageAllowed(): Boolean {
        val ops = activity.getSystemService(Context.APP_OPS_SERVICE) as AppOpsManager
        return ops.checkOpNoThrow(AppOpsManager.OPSTR_GET_USAGE_STATS,
            Process.myUid(), activity.packageName) == AppOpsManager.MODE_ALLOWED
    }

    private fun loadUsage() {
        val generation = ++statsGeneration
        if (!::wifiStat.isInitialized || !::usageAppsColumn.isInitialized) return
        if (!usageAllowed()) {
            wifiStat.text = "الواي فاي\nيلزم إذن الاستخدام"
            mobileStat.text = "بيانات الهاتف\nيلزم إذن الاستخدام"
            usageInfo.text = "اسمح بالوصول إلى بيانات الاستخدام لعرض استهلاك التطبيقات."
            usageAppsColumn.removeAllViews()
            usageAccess.visibility = View.VISIBLE
            usageShowAll.visibility = View.GONE
            return
        }
        usageAccess.visibility = View.GONE
        wifiStat.text = "الواي فاي\nجارٍ التحميل…"
        mobileStat.text = "بيانات الهاتف\nجارٍ التحميل…"
        usageInfo.text = "جارٍ قراءة الاستهلاك من نظام أندرويد…"
        usageAppsColumn.removeAllViews()
        val period = usagePeriod
        thread(name="smart-control-usage-report") {
            try {
                val manager = activity.getSystemService(Context.NETWORK_STATS_SERVICE)
                    as NetworkStatsManager
                val cal = Calendar.getInstance()
                when (period) {
                    0 -> { /* today */ }
                    1 -> cal.add(Calendar.DAY_OF_YEAR, -6)
                    else -> cal.set(Calendar.DAY_OF_MONTH, 1)
                }
                cal.set(Calendar.HOUR_OF_DAY, 0)
                cal.set(Calendar.MINUTE, 0)
                cal.set(Calendar.SECOND, 0)
                cal.set(Calendar.MILLISECOND, 0)
                val start = cal.timeInMillis
                val end = System.currentTimeMillis()

                fun deviceTotal(type: Int): String = try {
                    val bucket = manager.querySummaryForDevice(type, null, start, end)
                    humanBytes(bucket.rxBytes + bucket.txBytes)
                } catch (_: Exception) { "غير متاح على هذا الهاتف" }

                // Query one summary per network, grouping by UID rather than doing
                // hundreds of slow per-app system calls on the main thread.
                fun byUid(type: Int): Map<Int, Long>? = try {
                    val stats = manager.querySummary(type, null, start, end)
                        ?: return null
                    val totals = mutableMapOf<Int, Long>()
                    val bucket = NetworkStats.Bucket()
                    try {
                        while (stats.hasNextBucket()) {
                            stats.getNextBucket(bucket)
                            val bytes = (bucket.rxBytes + bucket.txBytes).coerceAtLeast(0L)
                            totals[bucket.uid] = (totals[bucket.uid] ?: 0L) + bytes
                        }
                    } finally {
                        stats.close()
                    }
                    totals
                } catch (_: Exception) { null }
                val wifiText = deviceTotal(ConnectivityManager.TYPE_WIFI)
                val mobileText = deviceTotal(ConnectivityManager.TYPE_MOBILE)
                val wifiMap = byUid(ConnectivityManager.TYPE_WIFI)
                val mobileMap = byUid(ConnectivityManager.TYPE_MOBILE)
                val rows = if (wifiMap == null && mobileMap == null)
                    emptyList<AppUsage>()
                else {
                    apps.groupBy { it.uid }.mapNotNull { (uid, belonging) ->
                        val w = wifiMap?.get(uid) ?: 0L
                        val m = mobileMap?.get(uid) ?: 0L
                        if (w + m <= 0) null else {
                            val label = belonging.first().loadLabel(activity.packageManager)
                                .toString() + if (belonging.size > 1)
                                " (+${belonging.size - 1})" else ""
                            AppUsage(label, w, m)
                        }
                    }.sortedByDescending { it.wifi + it.mobile }
                }
                activity.runOnUiThread {
                    if (profile != 2 || generation != statsGeneration) return@runOnUiThread
                    wifiStat.text = "الواي فاي\n" + wifiText
                    mobileStat.text = "بيانات الهاتف\n" + mobileText
                    usageRows = rows
                    usageInfo.text = if (wifiMap == null && mobileMap == null)
                        "تعذر عرض بيانات كل تطبيق على هذا الجهاز. قد يقيّد أندرويد الوصول."
                    else "التطبيقات المستهلكة للبيانات: " + rows.size +
                        " • بعض استهلاك النظام لا يظهر هنا"
                    renderUsageRows()
                }
            } catch (_: Exception) {
                activity.runOnUiThread {
                    if (profile != 2 || generation != statsGeneration) return@runOnUiThread
                    wifiStat.text = "الواي فاي\nتعذر القراءة"
                    mobileStat.text = "بيانات الهاتف\nتعذر القراءة"
                    usageInfo.text = "تعذر قراءة التقرير. تحقق من صلاحية الاستخدام."
                    usageAppsColumn.removeAllViews()
                }
            }
        }
    }

    private fun humanBytes(bytes: Long): String {
        return if (bytes >= 1_073_741_824)
            String.format(Locale.US, "%.2f GB", bytes / 1_073_741_824.0)
        else String.format(Locale.US, "%.1f MB", bytes / 1_048_576.0)
    }

    private fun buildAdvanced() {
        val advanced = card()
        advanced.addView(styleText("⚙  إعدادات متقدمة", 19f, ink, true))
        advanced.addView(styleText("إيقاف الحظر • قواعد الشبكات • الصلاحيات", 12f, muted))
        val disable = action("إيقاف VPN المحلي وإلغاء جميع القيود", pale, false)
        advanced.addView(disable)
        disable.setOnClickListener {
            prefs.edit().clear().apply()
            activity.stopService(Intent(activity, LocalBlockVpnService::class.java))
            showProfile(profile)
            Toast.makeText(activity, "تم إلغاء قواعد الحظر وإيقاف VPN", Toast.LENGTH_LONG).show()
        }
        root.addView(advanced)
    }
}
