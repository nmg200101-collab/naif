package com.smartscreen.control

import android.app.Activity
import android.app.AlertDialog
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
import android.widget.AdapterView
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
    private var pendingBackgroundAction: (() -> Unit)? = null
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
            setFillViewport(true)
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
    private fun defaultMode(network: String): String {
        val configured = prefs.getString("default_" + network, null)
        return if (configured != null) NetworkRulePolicy.normalize(configured)
            else if (prefs.getBoolean("all_" + network, false))
                NetworkRulePolicy.DENY else NetworkRulePolicy.ALLOW
    }

    private fun ruleMode(pkg: String, network: String = key()): String {
        val override = prefs.getString(keyFor(pkg, network), null)
        return if (override != null) NetworkRulePolicy.normalize(override)
            else defaultMode(network)
    }

    private fun anyRules(): Boolean {
        return listOf("wifi", "mobile").any { network ->
            NetworkRulePolicy.needsEnforcement(defaultMode(network)) ||
                apps.any { app ->
                    NetworkRulePolicy.needsEnforcement(ruleMode(app.packageName, network)) ||
                    prefs.getInt("quota_" + network + "_" + app.packageName, 0) > 0
                }
        }
    }

    private fun quotaKey(pkg: String, network: String = key()) =
        "quota_" + network + "_" + pkg
    private fun quotaHitKey(pkg: String, network: String = key()) =
        "quota_trip_" + network + "_" + pkg
    private fun quotaText(mb: Int): String = when (mb) {
        0 -> "بدون حد"
        1024 -> "1 جيجابايت يوميًا"
        else -> mb.toString() + " ميجابايت يوميًا"
    }

    private fun todayCode(): Int {
        val now = Calendar.getInstance()
        return DailyQuotaPolicy.dayCode(now.get(Calendar.YEAR),
            now.get(Calendar.DAY_OF_YEAR))
    }

    private fun chooseQuotaFor(packages: List<String>) {
        val selected = packages.filter { pkg -> apps.any { it.packageName == pkg } }
        if (selected.isEmpty()) {
            Toast.makeText(activity, "حدد تطبيقًا أولًا", Toast.LENGTH_SHORT).show()
            return
        }
        if (!usageAllowed()) {
            pendingBackgroundAction = { chooseQuotaFor(selected) }
            Toast.makeText(activity,
                "حد الاستهلاك يحتاج إذن الوصول إلى بيانات الاستخدام؛ ارجع بعد منحه.",
                Toast.LENGTH_LONG).show()
            try {
                activity.startActivity(Intent(Settings.ACTION_USAGE_ACCESS_SETTINGS))
            } catch (_: Exception) { pendingBackgroundAction = null }
            return
        }
        val presets = DailyQuotaPolicy.presetMb
        val labels = presets.map { quotaText(it) }.toTypedArray()
        val current = if (selected.size == 1)
            prefs.getInt(quotaKey(selected.first()), 0) else 0
        val index = presets.indexOf(current).coerceAtLeast(0)
        val title = if (selected.size == 1) "حد الاستهلاك اليومي للتطبيق"
            else "حد الاستهلاك اليومي لـ " + selected.size + " تطبيق"
        AlertDialog.Builder(activity)
            .setTitle(title)
            .setSingleChoiceItems(labels, index) { dialog, choice ->
                val mb = presets[choice]
                val network = key()
                val edit = prefs.edit()
                selected.forEach { pkg ->
                    edit.putInt(quotaKey(pkg, network), mb)
                    // Raising/removing a limit should immediately release an old trip.
                    edit.remove(quotaHitKey(pkg, network))
                }
                edit.apply()
                dialog.dismiss()
                selectedPackages.clear()
                renderApps()
                applyBlockerIfReady()
                Toast.makeText(activity,
                    if (mb == 0) "أُلغي حد الاستهلاك" else
                        "حُفظ حد الاستهلاك؛ فحص الاستخدام دوري وقد يتأخر",
                    Toast.LENGTH_LONG).show()
            }
            .setNegativeButton("إلغاء", null)
            .show()
    }

    private fun applyWithUsagePermission(mode: String, action: () -> Unit) {
        if (mode != NetworkRulePolicy.BACKGROUND || usageAllowed()) {
            action()
            return
        }
        pendingBackgroundAction = action
        Toast.makeText(activity, "حظر الخلفية يحتاج إذن الوصول إلى بيانات الاستخدام. "
            + "امنح الإذن ثم عد إلى التطبيق لإكمال الإجراء.", Toast.LENGTH_LONG).show()
        try {
            activity.startActivity(Intent(Settings.ACTION_USAGE_ACCESS_SETTINGS))
        } catch (_: Exception) {
            pendingBackgroundAction = null
            Toast.makeText(activity, "تعذر فتح شاشة الصلاحيات",
                Toast.LENGTH_LONG).show()
        }
    }

    private fun savePerAppMode(pkg: String, mode: String) {
        val network = key()
        applyWithUsagePermission(mode) {
            val edit = prefs.edit().putString(keyFor(pkg, network), mode)
            if (mode == NetworkRulePolicy.ALLOW) {
                edit.remove(quotaKey(pkg, network)).remove(quotaHitKey(pkg, network))
            }
            edit.apply()
            renderApps()
            applyBlockerIfReady()
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

        val backgroundOnly = action("☾  إيقاف الإنترنت في الخلفية فقط", pale, false)
        hero.addView(backgroundOnly)
        margin(backgroundOnly, 7)

        deny.setOnClickListener { changeRule(NetworkRulePolicy.DENY) }
        allow.setOnClickListener { changeRule(NetworkRulePolicy.ALLOW) }
        backgroundOnly.setOnClickListener { changeRule(NetworkRulePolicy.BACKGROUND) }

        val appCard = card()
        appCard.addView(styleText("▣  إدارة التطبيقات", 19f, ink, true))
        appCard.addView(styleText(
            "لكل تطبيق: السماح دائمًا • الخلفية فقط • حظر كامل. "
             + "قواعد الواي فاي والبيانات مستقلة.", 12f, muted))
        appCard.addView(styleText(
            "حد البيانات اليومي اختياري ويُقاس عبر نظام أندرويد. "
                + "قد تتأخر القراءة والحماية دقائق؛ ليس تحديدًا لسرعة الإنترنت.",
            12f, muted))
        selectionInfo = styleText("التطبيقات المحددة: 0", 12f, muted)
        appCard.addView(selectionInfo)
        val pickRow = linear(true)
        val allButton = action("تحديد الكل", pale, false)
        val noneButton = action("إلغاء التحديد", pale, false)
        pickRow.addView(allButton, LinearLayout.LayoutParams(0, dp(48), 1f))
        pickRow.addView(noneButton, LinearLayout.LayoutParams(0, dp(48), 1f))
        appCard.addView(pickRow)
        val applyRow = linear(true)
        val blockSelected = action("حظر كامل للمحدد", red)
        val allowSelected = action("السماح للمحدد", green)
        applyRow.addView(blockSelected, LinearLayout.LayoutParams(0, dp(48), 1f))
        applyRow.addView(allowSelected, LinearLayout.LayoutParams(0, dp(48), 1f))
        appCard.addView(applyRow)
        val backgroundSelected = action("حظر الخلفية للمحدد فقط", pale, false)
        appCard.addView(backgroundSelected)
        val dailyLimitSelected = action("حد الاستهلاك اليومي للمحدد", pale, false)
        appCard.addView(dailyLimitSelected)
        dailyLimitSelected.setOnClickListener {
            chooseQuotaFor(selectedPackages.toList())
        }

        allButton.setOnClickListener {
            selectedPackages.clear()
            selectedPackages.addAll(apps.map { it.packageName })
            renderApps()
        }
        noneButton.setOnClickListener {
            selectedPackages.clear()
            renderApps()
        }
        blockSelected.setOnClickListener { applySelectedRules(NetworkRulePolicy.DENY) }
        allowSelected.setOnClickListener { applySelectedRules(NetworkRulePolicy.ALLOW) }
        backgroundSelected.setOnClickListener { applySelectedRules(NetworkRulePolicy.BACKGROUND) }
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
            val row = linear().apply {
                background = bg(pale, 12)
                setPadding(dp(10), dp(6), dp(10), dp(6))
            }
            val name = app.loadLabel(activity.packageManager).toString()
            val heading = linear(true)
            val check = CheckBox(activity).apply {
                isChecked = selectedPackages.contains(app.packageName)
                contentDescription = "تحديد " + name + " للإجراء الجماعي"
                setOnCheckedChangeListener { _, selected ->
                    if (selected) selectedPackages.add(app.packageName)
                    else selectedPackages.remove(app.packageName)
                    updateSelectionInfo()
                }
            }
            heading.addView(check)
            heading.addView(styleText(name, 14f, ink, true),
                LinearLayout.LayoutParams(0, dp(44), 1f))
            row.addView(heading)
            val choices = listOf("السماح دائمًا", "الخلفية فقط", "حظر كامل")
            val values = listOf(NetworkRulePolicy.ALLOW,
                NetworkRulePolicy.BACKGROUND, NetworkRulePolicy.DENY)
            val spinner = Spinner(activity)
            spinner.adapter = ArrayAdapter(activity,
                android.R.layout.simple_spinner_dropdown_item, choices)
            val current = ruleMode(app.packageName)
            spinner.setSelection(values.indexOf(current).coerceAtLeast(0), false)
            spinner.contentDescription = "وضع الإنترنت لـ " + name
            spinner.onItemSelectedListener = object : AdapterView.OnItemSelectedListener {
                override fun onNothingSelected(parent: AdapterView<*>?) = Unit
                override fun onItemSelected(
                    parent: AdapterView<*>?, view: View?, position: Int, id: Long
                ) {
                    val chosen = values[position]
                    if (chosen != ruleMode(app.packageName)) {
                        savePerAppMode(app.packageName, chosen)
                    }
                }
            }
            row.addView(spinner, LinearLayout.LayoutParams(-1, dp(48)))
            val cap = prefs.getInt(quotaKey(app.packageName), 0)
            val reached = DailyQuotaPolicy.activeBlock(cap,
                prefs.getInt(quotaHitKey(app.packageName), 0), todayCode())
            val limitButton = action(
                (if (reached) "● وصل إلى الحد • " else "حد البيانات • ") +
                    quotaText(cap), pale, false)
            limitButton.textSize = 12f
            row.addView(limitButton, LinearLayout.LayoutParams(-1, dp(44)))
            limitButton.setOnClickListener {
                chooseQuotaFor(listOf(app.packageName))
            }
            appsColumn.addView(row)
            margin(row, 6)
        }
        updateSelectionInfo()
    }

    private fun updateSelectionInfo() {
        if (::selectionInfo.isInitialized) {
            selectionInfo.text = "المحدد: " + selectedPackages.size + " من " + apps.size +
                " • اختر ثم اضغط على وضع الحظر المطلوب"
        }
    }

    private fun applySelectedRules(mode: String) {
        if (selectedPackages.isEmpty()) {
            Toast.makeText(activity, "حدد تطبيقًا واحدًا على الأقل أولًا",
                Toast.LENGTH_SHORT).show()
            return
        }
        val chosen = selectedPackages.toList()
        val network = key()
        applyWithUsagePermission(mode) {
            val edit = prefs.edit()
            chosen.forEach {
                edit.putString(keyFor(it, network), mode)
                if (mode == NetworkRulePolicy.ALLOW) {
                    edit.remove(quotaKey(it, network))
                        .remove(quotaHitKey(it, network))
                }
            }
            edit.apply()
            selectedPackages.clear()
            renderApps()
            applyBlockerIfReady()
            val title = when(mode) {
                NetworkRulePolicy.BACKGROUND -> "حظر الخلفية"
                NetworkRulePolicy.DENY -> "الحظر الكامل"
                else -> "السماح"
            }
            Toast.makeText(activity, "تم تطبيق " + title + " على " +
                chosen.size + " تطبيق", Toast.LENGTH_LONG).show()
        }
    }

    private fun changeRule(mode: String) {
        val index = scope.selectedItemPosition
        val network = key()
        val appPackage = if (index > 0) apps.getOrNull(index - 1)?.packageName else null
        applyWithUsagePermission(mode) {
            if (appPackage == null) {
                val edit = prefs.edit()
                    .putString("default_" + network, mode)
                    .putBoolean("all_" + network, mode == NetworkRulePolicy.DENY)
                // Global commands reset previous per-app exceptions predictably.
                prefs.all.keys.filter { it.startsWith("rule_" + network + "_") ||
                    (mode == NetworkRulePolicy.ALLOW &&
                     (it.startsWith("quota_" + network + "_") ||
                      it.startsWith("quota_trip_" + network + "_"))) }
                    .forEach { edit.remove(it) }
                edit.apply()
            } else {
                val edit = prefs.edit().putString(keyFor(appPackage, network), mode)
                if (mode == NetworkRulePolicy.ALLOW) {
                    edit.remove(quotaKey(appPackage, network))
                        .remove(quotaHitKey(appPackage, network))
                }
                edit.apply()
            }
            renderApps()
            applyBlockerIfReady()
        }
    }

    private fun applyBlockerIfReady() {
        updateAccessStatus()
        if (!anyRules()) {
            activity.stopService(Intent(activity, LocalBlockVpnService::class.java))
            return
        }
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
        if (pendingBackgroundAction != null && usageAllowed()) {
            val pending = pendingBackgroundAction
            pendingBackgroundAction = null
            pending?.invoke()
        }
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
        val fullyBlocked = apps.count {
            ruleMode(it.packageName) == NetworkRulePolicy.DENY
        }
        val backgroundOnly = apps.count {
            ruleMode(it.packageName) == NetworkRulePolicy.BACKGROUND
        }
        val budgets = apps.count { prefs.getInt(quotaKey(it.packageName), 0) > 0 }
        val reached = apps.count { pkg ->
            val limit = prefs.getInt(quotaKey(pkg.packageName), 0)
            DailyQuotaPolicy.activeBlock(limit,
                prefs.getInt(quotaHitKey(pkg.packageName), 0), todayCode())
        }
        val rules = "حظر كامل: " + fullyBlocked + " • الخلفية: " +
            backgroundOnly + " • حدود يومية: " + budgets +
            (if (reached > 0) " • وصل للحد: " + reached else "")
        val state = when {
            LocalBlockVpnService.isMonitoring -> "حماية الإنترنت نشطة • " + rules
            fullyBlocked + backgroundOnly + budgets > 0 ->
                "القواعد محفوظة؛ VPN غير نشط • " + rules
            else -> "الإنترنت مسموح للتطبيقات"
        }
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
                fun byUid(type: Int): Map<Int, Long>? {
                    return try {
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
                }
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
