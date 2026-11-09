package com.smartscreen.control

import android.app.AppOpsManager
import android.app.KeyguardManager
import android.app.usage.UsageEvents
import android.app.usage.UsageStatsManager
import android.content.Context
import android.os.Handler
import android.os.HandlerThread
import android.os.Process
import android.os.PowerManager

/**
 * Best-effort foreground observer based on Android usage events.
 * It does NOT claim to detect every multi-window or picture-in-picture case.
 * Background-mode rules fail open if usage access is unavailable or event state
 * cannot be reconstructed, to avoid accidentally blocking an app in use.
 */
class ForegroundAppWatcher(
    private val context: Context,
    private val onChange: (Snapshot) -> Unit
) {
    data class Snapshot(val foregroundPackage: String?, val reliable: Boolean)
    @Volatile var snapshot = Snapshot(null, false)
        private set

    private val usage by lazy {
        context.getSystemService(Context.USAGE_STATS_SERVICE) as UsageStatsManager
    }
    private val power by lazy {
        context.getSystemService(Context.POWER_SERVICE) as PowerManager
    }
    private val keyguard by lazy {
        context.getSystemService(Context.KEYGUARD_SERVICE) as KeyguardManager
    }
    private val worker = HandlerThread("smart-control-foreground").apply { start() }
    private val handler = Handler(worker.looper)
    private var lastProcessed = 0L
    private var latestApp: String? = null
    private var anyEvent = false
    private var stopped = false

    private val scan = object : Runnable {
        override fun run() {
            if (stopped) return
            val next = detect()
            if (snapshot != next) {
                snapshot = next
                onChange(next)
            }
            if (!stopped) handler.postDelayed(this, 1800L)
        }
    }

    fun start() = handler.post(scan)

    fun stop() {
        stopped = true
        handler.removeCallbacksAndMessages(null)
        worker.quitSafely()
    }

    private fun hasPermission(): Boolean = try {
        val ops = context.getSystemService(Context.APP_OPS_SERVICE) as AppOpsManager
        ops.checkOpNoThrow(
            AppOpsManager.OPSTR_GET_USAGE_STATS,
            Process.myUid(),
            context.packageName
        ) == AppOpsManager.MODE_ALLOWED
    } catch (_: Exception) { false }

    private fun detect(): Snapshot {
        if (!power.isInteractive || keyguard.isKeyguardLocked) {
            // Screen off or keyguard active: none of the monitored apps is being used.
            return Snapshot(null, true)
        }
        if (!hasPermission()) return Snapshot(null, false)
        val now = System.currentTimeMillis()
        val start = if (lastProcessed == 0L) now - (6 * 60 * 60 * 1000L)
                    else (lastProcessed - 2000L).coerceAtLeast(0L)
        try {
            val events = usage.queryEvents(start, now) ?: return Snapshot(null, false)
            val event = UsageEvents.Event()
            while (events.hasNextEvent()) {
                events.getNextEvent(event)
                val timestamp = event.timeStamp
                if (timestamp < lastProcessed) continue
                when (event.eventType) {
                    UsageEvents.Event.ACTIVITY_RESUMED -> {
                        latestApp = event.packageName
                        anyEvent = true
                        lastProcessed = timestamp
                    }
                    UsageEvents.Event.ACTIVITY_PAUSED, UsageEvents.Event.ACTIVITY_STOPPED -> {
                        // A launcher or a paused activity may be followed by a resumed app.
                        // Apply pause only to the currently tracked app.
                        if (latestApp == event.packageName) latestApp = null
                        anyEvent = true
                        lastProcessed = timestamp
                    }
                }
            }
            return Snapshot(latestApp, anyEvent)
        } catch (_: Exception) {
            return Snapshot(null, false)
        }
    }
}
