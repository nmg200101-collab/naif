package com.smartscreen.control

import android.accessibilityservice.AccessibilityService
import android.content.Intent
import android.graphics.Color
import android.graphics.PixelFormat
import android.os.Build
import android.os.Handler
import android.os.Looper
import android.text.InputType
import android.view.Gravity
import android.view.View
import android.view.WindowManager
import android.view.accessibility.AccessibilityEvent
import android.widget.Button
import android.widget.EditText
import android.widget.LinearLayout
import android.widget.TextView
import java.util.concurrent.atomic.AtomicInteger
import kotlin.concurrent.thread

/**
 * Opt-in app lock: watches foreground package names only. Does not read any
 * screen text, keystrokes, messages, browsing contents, or accessibility nodes.
 *
 * Accessibility overlays are an extra convenience barrier, not OS-grade isolation:
 * transitions, multi-window modes, OEM policies and service disablement can bypass it.
 */
class AppLockAccessibilityService : AccessibilityService() {
    companion object {
        @Volatile var connected = false
            private set
        @Volatile private var active: AppLockAccessibilityService? = null

        fun biometricAccepted(pkg: String): Boolean =
            active?.acceptBiometric(pkg) ?: false

        fun biometricCanceled(pkg: String) {
            active?.cancelBiometric(pkg)
        }
    }

    private val main = Handler(Looper.getMainLooper())
    private lateinit var vault: AppLockStore
    private lateinit var wm: WindowManager
    private var overlay: View? = null
    private var target: String? = null
    private var unlocked: String? = null
    private var pendingBiometric: String? = null
    private var status: TextView? = null
    private var input: EditText? = null
    private var generation = AtomicInteger(0)

    override fun onServiceConnected() {
        super.onServiceConnected()
        vault = AppLockStore(this)
        wm = getSystemService(WINDOW_SERVICE) as WindowManager
        active = this
        connected = true
    }

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        if (event?.eventType != AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED &&
            event?.eventType != AccessibilityEvent.TYPE_WINDOWS_CHANGED) return
        if (!::vault.isInitialized) return
        val pkg = event.packageName?.toString() ?: return
        if (pkg == packageName) return
        if (pkg == "com.android.systemui") return
        if (!vault.isProtected(pkg)) {
            unlocked = null
            pendingBiometric = null
            dismissOverlay()
            return
        }
        if (pkg == unlocked || pkg == pendingBiometric) return
        if (target != pkg || overlay == null) showOverlay(pkg)
    }

    private fun dp(value: Int): Int =
        (value * resources.displayMetrics.density + 0.5f).toInt()

    private fun text(s: String, size: Float, bold: Boolean = false): TextView =
        TextView(this).apply {
            text = s
            textSize = size
            setTextColor(0xFF173C43.toInt())
            gravity = Gravity.CENTER
            setPadding(dp(8), dp(9), dp(8), dp(9))
            if (bold) setTypeface(null, android.graphics.Typeface.BOLD)
        }

    private fun button(s: String, callback: () -> Unit): Button =
        Button(this).apply {
            text = s
            textSize = 15f
            isAllCaps = false
            setTextColor(Color.WHITE)
            backgroundTintList = android.content.res.ColorStateList.valueOf(
                0xFF0E9F79.toInt())
            setOnClickListener { callback() }
        }

    private fun add(parent: LinearLayout, child: View, height: Int = -2) {
        parent.addView(child, LinearLayout.LayoutParams(-1, height))
    }

    private fun showOverlay(pkg: String) {
        dismissOverlay()
        target = pkg
        val label = try {
            @Suppress("DEPRECATION")
            packageManager.getApplicationLabel(packageManager.getApplicationInfo(pkg, 0))
                .toString()
        } catch (_: Exception) { "التطبيق المحمي" }
        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            layoutDirection = View.LAYOUT_DIRECTION_RTL
            gravity = Gravity.CENTER
            setBackgroundColor(0xFFF2FBF7.toInt())
            setPadding(dp(25), dp(45), dp(25), dp(45))
            isFocusable = true
            isFocusableInTouchMode = true
            contentDescription = "قفل التطبيقات — " + label
        }
        add(root, text("♢", 43f, true))
        add(root, text("التحكم الذكي", 22f, true))
        add(root, text("تطبيق محمي: " + label, 18f, true))
        add(root, text("أدخل رمز هذا التطبيق للمتابعة", 13f))
        status = text("", 13f)
        add(root, status!!)

        val kind = vault.kindFor(pkg) ?: LockCrypto.PIN
        if (kind == LockCrypto.PATTERN) {
            add(root, text("ارسم النقش بالترتيب (4 نقاط على الأقل)", 13f))
            val pattern = PatternDrawView(this)
            pattern.onPatternFinished = { entered -> verify(pkg, entered) }
            add(root, pattern, dp(240))
        } else {
            val code = EditText(this).apply {
                hint = if (kind == LockCrypto.PIN) "الرمز الرقمي" else "كلمة المرور"
                textSize = 20f
                gravity = Gravity.CENTER
                inputType = if (kind == LockCrypto.PIN)
                    InputType.TYPE_CLASS_NUMBER or InputType.TYPE_NUMBER_VARIATION_PASSWORD
                else InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD
                setSingleLine(true)
                setPadding(dp(5), dp(16), dp(5), dp(16))
                importantForAutofill = View.IMPORTANT_FOR_AUTOFILL_NO
            }
            input = code
            add(root, code)
            add(root, button("فتح التطبيق") {
                val entered = code.text.toString()
                code.text.clear()
                verify(pkg, entered)
            }, dp(56))
        }

        if (Build.VERSION.SDK_INT >= 28 && vault.biometricAllowed()) {
            add(root, button("فتح ببصمة / مصادقة الهاتف") { startBiometric(pkg) }, dp(56))
        }
        add(root, button("الخروج إلى الشاشة الرئيسية") {
            performGlobalAction(GLOBAL_ACTION_HOME)
            unlocked = null
            pendingBiometric = null
            dismissOverlay()
        }, dp(54))
        add(root, text("حماية إضافية تحتاج خدمة إمكانية الوصول. "
            + "لا تحل محل حماية نظام أندرويد الأصلية.", 11f))

        val flags = WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN or
            WindowManager.LayoutParams.FLAG_SECURE
        val params = WindowManager.LayoutParams(
            WindowManager.LayoutParams.MATCH_PARENT,
            WindowManager.LayoutParams.MATCH_PARENT,
            WindowManager.LayoutParams.TYPE_ACCESSIBILITY_OVERLAY,
            flags,
            PixelFormat.TRANSLUCENT
        ).apply {
            gravity = Gravity.TOP
            softInputMode = WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE
        }
        try {
            wm.addView(root, params)
            overlay = root
        } catch (_: Exception) {
            // Fail open when an OEM rejects the overlay; report through Settings UI.
            target = null
        }
    }

    private fun verify(pkg: String, entered: String) {
        if (pkg != target || overlay == null || entered.isEmpty()) return
        val delay = vault.lockoutRemainingMs(pkg)
        if (delay > 0) {
            status?.text = "محاولات كثيرة؛ أعد المحاولة بعد " +
                ((delay + 999) / 1000).toString() + " ثانية"
            return
        }
        status?.text = "جاري التحقق..."
        val token = generation.incrementAndGet()
        thread(name = "app-lock-pbkdf2") {
            val success = vault.verifyFor(pkg, entered)
            main.post {
                if (token != generation.get() || target != pkg) return@post
                vault.recordAttempt(pkg, success)
                if (success) {
                    unlocked = pkg
                    dismissOverlay()
                } else {
                    val remaining = vault.lockoutRemainingMs(pkg)
                    status?.text = if (remaining > 0)
                        "تم تعليق المحاولات لمدة 30 ثانية"
                    else "رمز غير صحيح؛ أعد المحاولة"
                }
            }
        }
    }

    private fun startBiometric(pkg: String) {
        if (pkg != target || Build.VERSION.SDK_INT < 28) return
        pendingBiometric = pkg
        dismissOverlay()
        try {
            startActivity(Intent(this, AppBiometricActivity::class.java)
                .putExtra("locked_pkg", pkg)
                .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK))
        } catch (_: Exception) {
            pendingBiometric = null
            showOverlay(pkg)
        }
    }

    private fun acceptBiometric(pkg: String): Boolean {
        if (pendingBiometric != pkg || !vault.isProtected(pkg) ||
            !vault.biometricAllowed()) return false
        pendingBiometric = null
        unlocked = pkg
        dismissOverlay()
        return true
    }

    private fun cancelBiometric(pkg: String) {
        if (pendingBiometric != pkg) return
        pendingBiometric = null
        if (vault.isProtected(pkg)) showOverlay(pkg)
    }

    private fun dismissOverlay() {
        generation.incrementAndGet()
        val previous = overlay
        overlay = null
        target = null
        input = null
        status = null
        if (previous != null) {
            try { wm.removeView(previous) } catch (_: Exception) {}
        }
    }

    override fun onInterrupt() { dismissOverlay() }

    override fun onDestroy() {
        connected = false
        if (active === this) active = null
        dismissOverlay()
        super.onDestroy()
    }
}
