package com.smartscreen.control

import android.app.AppOpsManager
import android.app.usage.NetworkStats
import android.app.usage.NetworkStatsManager
import android.content.Context
import android.content.SharedPreferences
import android.content.pm.ApplicationInfo
import android.net.ConnectivityManager
import android.os.Handler
import android.os.HandlerThread
import android.os.Looper
import android.os.Process
import java.util.Calendar

/**
 * Advisory daily data caps, sampled about once every two minutes.
 *
 * When Android supplies usage statistics, a reached cap sets a per-app block rule
 * for the current calendar day. It is NOT an instantaneous cap, a hard guarantee
 * of consumed bytes, or an upload/download speed limiter.
 *
 * Failed/denied readings never introduce new blocks. Shared-UID apps are skipped
 * because usage cannot be attributed to a unique package.
 */
class DailyQuotaMonitor(
    private val context: Context,
    private val prefs: SharedPreferences,
    private val changed: () -> Unit
) {
    private val worker = HandlerThread("smart-control-usage-caps").apply { start() }
    private val handler = Handler(worker.looper)
    private val main = Handler(Looper.getMainLooper())
    @Volatile private var stopped = false
    private var lastObservedDay = 0
    private val runnable = object : Runnable {
        override fun run() {
            if (stopped) return
            try { sample() } catch (_: Exception) { /* fail open */ }
            if (!stopped) handler.postDelayed(this, 120_000L)
        }
    }

    fun start() { handler.post(runnable) }
    fun resample() {
        if (!stopped) {
            handler.removeCallbacks(runnable)
            handler.post(runnable)
        }
    }
    fun stop() {
        stopped = true
        handler.removeCallbacksAndMessages(null)
        worker.quitSafely()
    }

    private fun usageAllowed(): Boolean = try {
        val ops = context.getSystemService(Context.APP_OPS_SERVICE) as AppOpsManager
        ops.checkOpNoThrow(AppOpsManager.OPSTR_GET_USAGE_STATS,
            Process.myUid(), context.packageName) == AppOpsManager.MODE_ALLOWED
    } catch (_: Exception) { false }

    private fun sample() {
        if (!usageAllowed()) return
        val selected = listOf("wifi", "mobile").flatMap { network ->
            val prefix = "quota_" + network + "_"
            prefs.all.entries.mapNotNull { (key, value) ->
                if (!key.startsWith(prefix)) null
                else {
                    val mb = (value as? Int) ?: 0
                    if (mb <= 0) null else Triple(network, key.removePrefix(prefix), mb)
                }
            }
        }
        if (selected.isEmpty()) return

        @Suppress("DEPRECATION")
        val visible = context.packageManager.getInstalledApplications(0)
            .filter {
                it.packageName != context.packageName &&
                    ((it.flags and ApplicationInfo.FLAG_SYSTEM) == 0 ||
                    context.packageManager.getLaunchIntentForPackage(it.packageName) != null)
            }
        val byName = visible.associateBy { it.packageName }
        val countByUid = visible.groupingBy { it.uid }.eachCount()
        val statsManager =
            context.getSystemService(Context.NETWORK_STATS_SERVICE) as NetworkStatsManager
        val calendar = Calendar.getInstance()
        val today = DailyQuotaPolicy.dayCode(
            calendar.get(Calendar.YEAR), calendar.get(Calendar.DAY_OF_YEAR))
        calendar.set(Calendar.HOUR_OF_DAY, 0)
        calendar.set(Calendar.MINUTE, 0)
        calendar.set(Calendar.SECOND, 0)
        calendar.set(Calendar.MILLISECOND, 0)
        val from = calendar.timeInMillis
        val until = System.currentTimeMillis()

        fun usageByUid(type: Int): Map<Int, Long>? {
            return try {
            val stream = statsManager.querySummary(type, null, from, until)
                ?: return null
            val result = HashMap<Int, Long>()
            val bucket = NetworkStats.Bucket()
            try {
                while (stream.hasNextBucket()) {
                    stream.getNextBucket(bucket)
                    result[bucket.uid] = (result[bucket.uid] ?: 0L) +
                        (bucket.rxBytes + bucket.txBytes).coerceAtLeast(0)
                }
            } finally {
                stream.close()
            }
            result
            } catch (_: Exception) { null }
        }

        val wifiUsage = if (selected.any { it.first == "wifi" })
            usageByUid(ConnectivityManager.TYPE_WIFI) else null
        val mobileUsage = if (selected.any { it.first == "mobile" })
            usageByUid(ConnectivityManager.TYPE_MOBILE) else null

        var updated = false
        val editor = prefs.edit()
        for ((network, pkg, megabytes) in selected) {
            val app = byName[pkg] ?: continue
            if ((countByUid[app.uid] ?: 0) != 1) continue
            val totals = if (network == "wifi") wifiUsage else mobileUsage
            val used = totals?.get(app.uid) ?: continue
            if (DailyQuotaPolicy.reached(megabytes, used) &&
                prefs.getInt("quota_trip_" + network + "_" + pkg, 0) != today) {
                editor.putInt("quota_trip_" + network + "_" + pkg, today)
                updated = true
            }
        }
        val dayChanged = lastObservedDay != 0 && lastObservedDay != today
        lastObservedDay = today
        if (updated) editor.apply()
        if (updated || dayChanged) main.post { if (!stopped) changed() }
    }
}
