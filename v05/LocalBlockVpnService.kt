package com.smartscreen.control

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.ApplicationInfo
import android.content.pm.PackageManager
import android.content.pm.ServiceInfo
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import android.net.NetworkRequest
import android.net.VpnService
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.os.ParcelFileDescriptor
import java.io.FileInputStream
import kotlin.concurrent.thread
import java.util.Calendar

/**
 * Local *blocking-only* VPN. Only applications marked blocked are routed to the local
 * TUN interface; all other applications bypass this tunnel and retain normal internet.
 * This VPN does not forward packets, decrypt data, inspect payloads or accelerate traffic.
 *
 * Intended as a limited app-level blocker, not a universal firewall for hotspot/system apps.
 * Security-critical invariant: never call establish() without at least one app in
 * addAllowedApplication(), otherwise Android routes ALL apps into the blackhole.
 */
class LocalBlockVpnService : VpnService() {
    companion object {
        const val ACTION_APPLY = "com.smartscreen.control.APPLY_BLOCK_RULES"
        const val ACTION_STOP = "com.smartscreen.control.STOP_BLOCK_RULES"
        @Volatile var isRunning = false
            private set
        @Volatile var isMonitoring = false
            private set
    }

    private val prefs by lazy { getSharedPreferences("network_rules_v05", MODE_PRIVATE) }
    private val connectivity by lazy {
        getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
    }
    private val main = Handler(Looper.getMainLooper())
    private val notificationId = 9152
    private val channelId = "internet_blocker_v05"
    private var tunnel: ParcelFileDescriptor? = null
    private var pump: Thread? = null
    private var activeSignature = ""
    private var activeTransport = ""
    private var foregroundWatcher: ForegroundAppWatcher? = null
    private var quotaMonitor: DailyQuotaMonitor? = null
    private var networkCallback: ConnectivityManager.NetworkCallback? = null

    override fun onCreate() {
        super.onCreate()
        val request = NetworkRequest.Builder()
            .addCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
            .build()
        val callback = object : ConnectivityManager.NetworkCallback() {
            override fun onAvailable(network: Network) { scheduleProfileUpdate() }
            override fun onLost(network: Network) { scheduleProfileUpdate() }
            override fun onCapabilitiesChanged(network: Network, caps: NetworkCapabilities) {
                if (caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) ||
                    caps.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR)) scheduleProfileUpdate()
            }
        }
        try { connectivity.registerNetworkCallback(request, callback); networkCallback = callback }
        catch (_: Exception) {}
        foregroundWatcher = ForegroundAppWatcher(this) { snapshot ->
            main.post {
                if (isMonitoring) installRules()
            }
        }.also { it.start() }
        quotaMonitor = DailyQuotaMonitor(this, prefs) {
            main.post { if (isMonitoring) installRules() }
        }.also { it.start() }
    }

    private fun scheduleProfileUpdate() {
        main.removeCallbacks(updateRunnable)
        main.postDelayed(updateRunnable, 1100)
    }
    private val updateRunnable = Runnable {
        if (isMonitoring) installRules()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        if (intent?.action == ACTION_STOP) {
            stopSelf()
            return START_NOT_STICKY
        }
        try {
            createNotificationChannel()
            val note = makeNotification("فحص قواعد الحظر")
            if (Build.VERSION.SDK_INT >= 34) {
                startForeground(notificationId, note,
                    ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE)
            } else {
                startForeground(notificationId, note)
            }
            isMonitoring = true
            installRules()
            quotaMonitor?.resample()
        } catch (_: Exception) {
            stopSelf()
        }
        return START_NOT_STICKY
    }

    /** Prefer validated Wi-Fi over cellular; never mistake the VPN itself for Wi-Fi. */
    private fun currentTransport(): String? {
        var cell = false
        try {
            for (network in connectivity.allNetworks) {
                val caps = connectivity.getNetworkCapabilities(network) ?: continue
                if (caps.hasTransport(NetworkCapabilities.TRANSPORT_VPN) ||
                    !caps.hasCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)) continue
                if (caps.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) &&
                    caps.hasCapability(NetworkCapabilities.NET_CAPABILITY_VALIDATED)) return "wifi"
                if (caps.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR)) cell = true
            }
        } catch (_: Exception) {}
        return if (cell) "mobile" else null
    }

    private fun defaultMode(network: String): String {
        val configured = prefs.getString("default_" + network, null)
        return if (configured != null) NetworkRulePolicy.normalize(configured)
            else if (prefs.getBoolean("all_" + network, false))
                NetworkRulePolicy.DENY else NetworkRulePolicy.ALLOW
    }

    private fun ruleMode(network: String, pkg: String): String =
        NetworkRulePolicy.normalize(
            prefs.getString("rule_" + network + "_" + pkg, null) ?: defaultMode(network))

    private fun supportedApplications(): List<ApplicationInfo> {
        @Suppress("DEPRECATION")
        return packageManager.getInstalledApplications(0)
            .filter {
                it.packageName != packageName &&
                    ((it.flags and ApplicationInfo.FLAG_SYSTEM) == 0 ||
                     packageManager.getLaunchIntentForPackage(it.packageName) != null)
            }
            .sortedBy { it.packageName }
            .take(320)
    }

    private fun packagesForTransport(transport: String): List<String> {
        val state = foregroundWatcher?.snapshot
        val now = Calendar.getInstance()
        val today = DailyQuotaPolicy.dayCode(
            now.get(Calendar.YEAR), now.get(Calendar.DAY_OF_YEAR))
        return supportedApplications()
            .filter { app ->
                val pkg = app.packageName
                val modeBlocked = NetworkRulePolicy.isBlocked(
                    mode = ruleMode(transport, pkg),
                    packageName = pkg,
                    foregroundPackage = state?.foregroundPackage,
                    foregroundReliable = state?.reliable ?: false
                )
                val limit = prefs.getInt("quota_" + transport + "_" + pkg, 0)
                val reachedDay = prefs.getInt("quota_trip_" + transport + "_" + pkg, 0)
                modeBlocked || DailyQuotaPolicy.activeBlock(limit, reachedDay, today)
            }
            .map { it.packageName }
    }

    @Synchronized
    private fun installRules() {
        val transport = currentTransport()
        if (transport == null) {
            closeTunnel()
            activeTransport = ""
            activeSignature = ""
            // Retain monitoring while temporarily offline; the connection callback
            // will reactivate rules when Wi-Fi/mobile data becomes available.
            if (!hasAnyRules()) stopSelf()
            return
        }
        val packages = packagesForTransport(transport)
        val signature = transport + ":" + packages.sorted().joinToString("|")
        if (signature == activeSignature &&
            (tunnel != null || packages.isEmpty())) return
        closeTunnel()
        activeSignature = signature
        activeTransport = transport
        if (packages.isEmpty()) {
            // No traffic should ever be sent to a blackhole with an empty allow-list.
            updateNotification(
                if (hasAnyRules())
                    "الحماية نشطة • التطبيق المفتوح مسموح، أو لا توجد تطبيقات محظورة الآن"
                else "الحظر متوقف • جميع التطبيقات مسموح لها")
            if (!hasAnyRules()) stopSelf()
            return
        }
        try {
            val builder = Builder()
                .setSession("التحكم الذكي — حظر التطبيقات")
                .setMtu(1500)
                .addAddress("10.254.42.1", 32)
                .addRoute("0.0.0.0", 0)
                .addAddress("fd12:3456:789a::1", 128)
                .addRoute("::", 0)
            var added = 0
            for (pkg in packages) {
                try {
                    builder.addAllowedApplication(pkg)
                    added++
                } catch (_: PackageManager.NameNotFoundException) {}
            }
            if (added == 0) {
                updateNotification("تعذر العثور على التطبيقات المختارة")
                return
            }
            val fd = builder.establish() ?: return
            tunnel = fd
            isRunning = true
            pump = thread(name = "smart-control-packet-dropper", isDaemon = true) {
                val data = ByteArray(32767)
                try {
                    FileInputStream(fd.fileDescriptor).use { input ->
                        while (!Thread.currentThread().isInterrupted) {
                            val size = input.read(data)
                            if (size < 0) break
                            if (size == 0) Thread.sleep(25)
                            // Intentional drop of packets from blocked applications.
                        }
                    }
                } catch (_: Exception) {
                    // Closing the TUN stops the read loop.
                }
            }
            val state = foregroundWatcher?.snapshot
            val note = "حظر " + added + " تطبيق • " +
                (if (transport == "wifi") "واي فاي" else "بيانات الهاتف") +
                if (state?.reliable == false && hasBackgroundRules())
                    " • حظر الخلفية ينتظر إذن الاستخدام" else ""
            updateNotification(note)
        } catch (_: Exception) {
            closeTunnel()
            updateNotification("فشل تفعيل الحظر؛ أوقف VPN وأعد المحاولة")
        }
    }

    private fun hasAnyRules(): Boolean {
        return listOf("wifi", "mobile").any { network ->
            NetworkRulePolicy.needsEnforcement(defaultMode(network)) ||
                supportedApplications().any {
                    NetworkRulePolicy.needsEnforcement(ruleMode(network, it.packageName)) ||
                    prefs.getInt("quota_" + network + "_" + it.packageName, 0) > 0
                }
        }
    }

    private fun hasBackgroundRules(): Boolean {
        return listOf("wifi", "mobile").any { network ->
            defaultMode(network) == NetworkRulePolicy.BACKGROUND ||
                supportedApplications().any {
                    ruleMode(network, it.packageName) == NetworkRulePolicy.BACKGROUND
                }
        }
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= 26) {
            (getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager)
                .createNotificationChannel(NotificationChannel(channelId,
                    "حماية الإنترنت المحلي", NotificationManager.IMPORTANCE_LOW))
        }
    }

    private fun makeNotification(subtitle: String): Notification {
        val open = PendingIntent.getActivity(this, 61,
            Intent(this, MainActivity::class.java),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
        val stop = PendingIntent.getService(this, 62,
            Intent(this, LocalBlockVpnService::class.java).setAction(ACTION_STOP),
            PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE)
        return Notification.Builder(this, channelId)
            .setSmallIcon(R.drawable.ic_app)
            .setContentTitle("التحكم الذكي — إدارة الإنترنت")
            .setContentText(subtitle)
            .setContentIntent(open)
            .addAction(android.R.drawable.ic_menu_close_clear_cancel, "فتح الإنترنت", stop)
            .setOngoing(true)
            .build()
    }

    private fun updateNotification(message: String) {
        (getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager)
            .notify(notificationId, makeNotification(message))
    }

    @Synchronized
    private fun closeTunnel() {
        try { tunnel?.close() } catch (_: Exception) {}
        tunnel = null
        pump?.interrupt()
        pump = null
        isRunning = false
    }

    override fun onRevoke() {
        closeTunnel()
        stopSelf()
    }

    override fun onDestroy() {
        isMonitoring = false
        foregroundWatcher?.stop()
        foregroundWatcher = null
        quotaMonitor?.stop()
        quotaMonitor = null
        main.removeCallbacks(updateRunnable)
        networkCallback?.let {
            try { connectivity.unregisterNetworkCallback(it) } catch (_: Exception) {}
        }
        networkCallback = null
        closeTunnel()
        super.onDestroy()
    }
}
