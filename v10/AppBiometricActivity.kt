package com.smartscreen.control

import android.app.Activity
import android.hardware.biometrics.BiometricPrompt
import android.os.Build
import android.os.Bundle
import android.os.CancellationSignal
import android.widget.Toast

/**
 * Optional Android-owned biometric dialog. This screen is not exported and
 * never receives or stores raw fingerprints. If Android rejects starting this
 * activity from the accessibility overlay, the app-lock code remains the fallback.
 */
class AppBiometricActivity : Activity() {
    private var targetPkg: String? = null
    private var accepted = false
    private var promptStarted = false
    private val cancellation = CancellationSignal()

    override fun onCreate(state: Bundle?) {
        super.onCreate(state)
        targetPkg = intent?.getStringExtra("locked_pkg")
        if (Build.VERSION.SDK_INT < 28 || targetPkg.isNullOrBlank()) {
            finish()
            return
        }
    }

    override fun onResume() {
        super.onResume()
        if (promptStarted || targetPkg == null || Build.VERSION.SDK_INT < 28) return
        promptStarted = true
        try {
            val prompt = BiometricPrompt.Builder(this)
                .setTitle("التحكم الذكي")
                .setSubtitle("تحقق من بصمة الهاتف لفتح التطبيق")
                .setNegativeButton("استخدام الرمز", mainExecutor) { _, _ -> finish() }
                .build()
            prompt.authenticate(cancellation, mainExecutor,
                object : BiometricPrompt.AuthenticationCallback() {
                    override fun onAuthenticationSucceeded(
                        result: BiometricPrompt.AuthenticationResult?
                    ) {
                        super.onAuthenticationSucceeded(result)
                        val pkg = targetPkg ?: return
                        accepted = AppLockAccessibilityService.biometricAccepted(pkg)
                        finish()
                    }
                    override fun onAuthenticationError(code: Int, msg: CharSequence?) {
                        super.onAuthenticationError(code, msg)
                        if (!isFinishing) {
                            Toast.makeText(this@AppBiometricActivity,
                                "المصادقة الحيوية غير متاحة؛ استخدم الرمز",
                                Toast.LENGTH_SHORT).show()
                            finish()
                        }
                    }
                })
        } catch (_: Exception) {
            Toast.makeText(this, "لم ينجح فتح مصادقة الهاتف؛ استخدم الرمز",
                Toast.LENGTH_SHORT).show()
            finish()
        }
    }

    override fun onDestroy() {
        if (!accepted) targetPkg?.let {
            AppLockAccessibilityService.biometricCanceled(it)
        }
        cancellation.cancel()
        super.onDestroy()
    }
}
