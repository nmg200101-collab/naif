package com.smartscreen.control

import android.Manifest
import android.app.Activity
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.graphics.Color
import android.graphics.Typeface
import android.graphics.drawable.GradientDrawable
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.net.wifi.WifiManager
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.provider.Settings
import android.view.Gravity
import android.view.View
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast

/**
 * Supported hotspot tools plus transparent capability disclosure. Normal Android
 * apps cannot arbitrarily block tethered clients or enumerate their identities.
 * A local-only hotspot NEVER provides internet access to clients.
 */
class HotspotPanel(
    private val activity: Activity,
    private val root: LinearLayout
) {
    private val blue = 0xFF248FDB.toInt()
    private val green = 0xFF13AC7B.toInt()
    private val ink = 0xFF17383D.toInt()
    private val muted = 0xFF6A8286.toInt()
    private val pale = 0xFFF3FBF8.toInt()
    private val wifi by lazy {
        @Suppress("DEPRECATION")
        activity.applicationContext.getSystemService(Context.WIFI_SERVICE) as WifiManager
    }
    private var reservation: WifiManager.LocalOnlyHotspotReservation? = null
    private var localSsid: String? = null
    private var localPassword: String? = null
    private var passwordVisible = false
    private var page = 0
    private var lastMessage = "نقطة بث الإنترنت تُدار من إعدادات النظام"
    private var statusView: TextView? = null

    private fun dp(i: Int) = (i * activity.resources.displayMetrics.density + 0.5f).toInt()
    private fun shape(color: Int = Color.WHITE) = GradientDrawable().apply {
        setColor(color)
        cornerRadius = dp(20).toFloat()
        setStroke(dp(1), 0xFFD7E8E6.toInt())
    }
    private fun stack() = LinearLayout(activity).apply {
        orientation = LinearLayout.VERTICAL
        layoutDirection = View.LAYOUT_DIRECTION_RTL
    }
    private fun label(value: String, size: Float = 14f,
                      bold: Boolean = false, color: Int = ink) =
        TextView(activity).apply {
            text = value
            textSize = size
            setTextColor(color)
            gravity = Gravity.RIGHT or Gravity.CENTER_VERTICAL
            setPadding(dp(4), dp(6), dp(4), dp(6))
            if (bold) typeface = Typeface.create(Typeface.DEFAULT, Typeface.BOLD)
        }
    private fun button(value: String, main: Boolean = false, action: () -> Unit): Button =
        Button(activity).apply {
            text = value
            textSize = 13f
            isAllCaps = false
            setTextColor(if (main) Color.WHITE else ink)
            backgroundTintList = android.content.res.ColorStateList.valueOf(
                if (main) green else 0xFFE7F4F0.toInt())
            setOnClickListener { action() }
        }
    private fun box(title: String, description: String): LinearLayout = stack().apply {
        background = shape()
        setPadding(dp(14), dp(13), dp(14), dp(13))
        val lp = LinearLayout.LayoutParams(-1, -2).apply { topMargin = dp(12) }
        layoutParams = lp
        addView(label(title, 18f, true))
        addView(label(description, 12f, false, muted))
    }
    private fun openSettings() {
        try {
            activity.startActivity(Intent("android.settings.TETHER_SETTINGS"))
        } catch (_: Exception) {
            try {
                activity.startActivity(Intent(Settings.ACTION_WIRELESS_SETTINGS))
            } catch (_: Exception) {
                Toast.makeText(activity,"تعذر فتح إعدادات نقطة البث",Toast.LENGTH_LONG).show()
            }
        }
    }
    private fun connectionSource(): String {
        return try {
            val manager = activity.getSystemService(Context.CONNECTIVITY_SERVICE)
                as ConnectivityManager
            val connected = manager.allNetworks.mapNotNull { manager.getNetworkCapabilities(it) }
                .filter { !it.hasTransport(NetworkCapabilities.TRANSPORT_VPN) }
            when {
                connected.any { it.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) &&
                    it.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET) } ->
                    "واي فاي متاح؛ مشاركة الإنترنت منه تعتمد على دعم الهاتف"
                connected.any { it.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) } ->
                    "بيانات الهاتف متاحة؛ تحقق من تفعيل مشاركة الإنترنت بالنظام"
                else -> "لا يتوفر مصدر إنترنت مؤكد حاليًا"
            }
        } catch (_: Exception) { "مصدر الاتصال غير متاح للقراءة" }
    }
    private fun permission(): String = if (Build.VERSION.SDK_INT >= 33)
        Manifest.permission.NEARBY_WIFI_DEVICES else Manifest.permission.ACCESS_FINE_LOCATION

    private fun startLocal() {
        if (reservation != null) {
            Toast.makeText(activity, "شبكة البث المحلية تعمل بالفعل", Toast.LENGTH_SHORT).show()
            return
        }
        val p = permission()
        if (activity.checkSelfPermission(p) != PackageManager.PERMISSION_GRANTED) {
            activity.requestPermissions(arrayOf(p), 907)
            lastMessage = "امنح إذن شبكة الواي فاي ثم اضغط إنشاء الشبكة المحلية مرة أخرى"
            redraw()
            return
        }
        lastMessage = "يتم طلب إنشاء شبكة محلية بدون إنترنت..."
        redraw()
        try {
            wifi.startLocalOnlyHotspot(object : WifiManager.LocalOnlyHotspotCallback() {
                override fun onStarted(value: WifiManager.LocalOnlyHotspotReservation) {
                    reservation = value
                    if (Build.VERSION.SDK_INT >= 30) {
                        val conf = value.softApConfiguration
                        localSsid = conf.ssid
                        localPassword = conf.passphrase
                    } else {
                        @Suppress("DEPRECATION")
                        val conf = value.wifiConfiguration
                        localSsid = conf?.SSID
                        localPassword = conf?.preSharedKey
                    }
                    lastMessage = "الشبكة المحلية نشطة — بدون إنترنت"
                    redraw()
                }
                override fun onStopped() {
                    reservation = null
                    localPassword = null
                    localSsid = null
                    lastMessage = "أوقف أندرويد الشبكة المحلية"
                    redraw()
                }
                override fun onFailed(reason: Int) {
                    reservation = null
                    lastMessage = "تعذر إنشاء شبكة محلية (رمز الخطأ: $reason)"
                    redraw()
                }
            }, Handler(Looper.getMainLooper()))
        } catch (_: SecurityException) {
            lastMessage = "صلاحية نقطة البث غير متاحة؛ استخدم إعدادات النظام"
            redraw()
        } catch (_: Exception) {
            lastMessage = "تعذر إنشاء نقطة بث محلية على هذا الهاتف"
            redraw()
        }
    }
    private fun stopLocal() {
        try { reservation?.close() } catch (_: Exception) {}
        reservation = null
        localPassword = null
        localSsid = null
        passwordVisible = false
        lastMessage = "تم طلب إيقاف الشبكة المحلية"
        redraw()
    }

    fun redraw() {
        root.removeAllViews()
        val banner = box("نقطة البث", "إنترنت مشترك أو شبكة محلية بدون إنترنت")
        root.addView(banner)
        val tabs = LinearLayout(activity).apply {
            orientation = LinearLayout.HORIZONTAL
            layoutDirection = View.LAYOUT_DIRECTION_RTL
        }
        val names = listOf("الحالة", "الأجهزة", "السماح", "الإعدادات", "السجل")
        for ((i,name) in names.withIndex()) {
            val chip = button(name) { page = i; redraw() }
            chip.textSize = 11f
            chip.backgroundTintList = android.content.res.ColorStateList.valueOf(
                if (page == i) 0xFFD7F5EA.toInt() else Color.WHITE)
            val lp = LinearLayout.LayoutParams(0, dp(49), 1f)
            tabs.addView(chip,lp)
        }
        root.addView(tabs)
        when(page) {
            0 -> renderStatus()
            1 -> root.addView(box("الأجهزة المتصلة",
                "لا يمكن قراءة قائمة أجهزة نقطة بث الإنترنت بصورة موثوقة من تطبيق عادي على معظم هواتف أندرويد. افتح إعدادات نقطة البث لعرضها إذا كان هاتفك يدعم ذلك."))
            2 -> root.addView(box("السماح والحظر",
                "التحكم الحقيقي في قائمة السماح والحظر أو فصل الأجهزة مرتبط بصلاحيات النظام والشركة المصنعة. لن نعرض حظرًا وهميًا أو جهازًا مختلقًا."))
            3 -> renderSettings()
            4 -> root.addView(box("سجل الاتصال",
                "قيد التطوير: لا يُنشأ سجل فعلي لاتصالات الأجهزة إلا بعد توفر واجهة اتصال معتمدة تعرضها صلاحيات هذا الهاتف."))
        }
        if (page == 1 || page == 2) {
            root.addView(button("فتح إدارة الأجهزة من إعدادات الهاتف",true) { openSettings() })
        }
    }
    private fun renderStatus() {
        val current = box("حالة الاتصال", connectionSource())
        statusView = label(lastMessage, 13f, true,
            if (reservation != null) green else muted)
        current.addView(statusView)
        current.addView(label("مشاركة الإنترنت من بيانات الهاتف أو الواي فاي تُضبط من النظام؛ بعض الأجهزة لا تدعم بث الواي فاي وإعادة مشاركته معًا.", 12f, false, muted))
        current.addView(button("فتح إعدادات مشاركة الإنترنت",true) { openSettings() })
        root.addView(current)
        val local = box("نقطة بث محلية", "للأجهزة القريبة فقط، دون مشاركة الإنترنت")
        local.addView(label("الحالة: " + if (reservation == null) "متوقفة" else "نشطة",13f,true))
        if (reservation != null) {
            local.addView(label("اسم الشبكة: " + (localSsid ?: "يحدده الهاتف"),13f))
            val pass = if (passwordVisible) (localPassword ?: "غير متاح") else "••••••••"
            local.addView(label("كلمة المرور: " + pass,13f))
            local.addView(button(if (passwordVisible) "إخفاء كلمة المرور" else "عرض كلمة المرور") {
                passwordVisible = !passwordVisible; redraw()
            })
            local.addView(button("إيقاف البث المحلي",true) { stopLocal() })
        } else {
            local.addView(button("إنشاء شبكة محلية بدون إنترنت",true) { startLocal() })
        }
        root.addView(local)
    }
    private fun renderSettings() {
        val card = box("إعدادات نقطة بث الإنترنت",
            "اسم الشبكة، كلمة المرور، التشفير، الأجهزة المسموحة، المهلة التلقائية وإعادة بث الواي فاي—كلها تعتمد على قوائم وقدرات الهاتف.")
        card.addView(button("فتح إعدادات نقطة البث",true) { openSettings() })
        card.addView(label("لا يطلب التطبيق صلاحيات مدير جهاز لتجاوز الحماية، ولا يعد بالحظر المباشر للأجهزة إن لم يسمح أندرويد بذلك.",12f,false,muted))
        root.addView(card)
    }
    fun cleanup() { stopLocal() }
}
