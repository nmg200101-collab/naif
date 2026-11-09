package com.smartscreen.control

import android.content.Context
import android.content.SharedPreferences
import android.util.Base64

/**
 * Private per-application lock configuration. Credential material is PBKDF2 data only.
 * Disabled by default; Android accessibility permission must be explicitly granted.
 */
class AppLockStore(context: Context) {
    private val p: SharedPreferences = context.getSharedPreferences(
        "smart_control_app_lock_v1", Context.MODE_PRIVATE)

    private fun id(pkg: String): String =
        Base64.encodeToString(pkg.toByteArray(Charsets.UTF_8),
            Base64.URL_SAFE or Base64.NO_WRAP or Base64.NO_PADDING)

    private fun itemKey(pkg: String) = "item_" + id(pkg)
    private fun methodKey(pkg: String) = "method_" + id(pkg)
    private fun failKey(pkg: String) = "failure_" + id(pkg)
    private fun untilKey(pkg: String) = "until_" + id(pkg)

    fun hasMaster(): Boolean = LockCrypto.kind(p.getString("master", null)) != null
    fun masterKind(): String? = LockCrypto.kind(p.getString("master", null))

    fun verifyMaster(secret: String): Boolean =
        LockCrypto.verify(secret, p.getString("master", null))

    fun setMaster(kind: String, secret: String) {
        val record = LockCrypto.create(kind, secret)
        p.edit().putString("master", record).apply()
    }

    fun protectedPackages(): Set<String> =
        (p.getStringSet("protected", emptySet()) ?: emptySet()).toSet()

    fun isProtected(pkg: String) = protectedPackages().contains(pkg)

    fun hasSeparate(pkg: String): Boolean =
        LockCrypto.kind(p.getString(itemKey(pkg), null)) != null

    fun method(pkg: String): String =
        if (hasSeparate(pkg)) "individual" else "master"

    fun kindFor(pkg: String): String? =
        if (hasSeparate(pkg)) LockCrypto.kind(p.getString(itemKey(pkg), null))
        else masterKind()

    fun verifyFor(pkg: String, secret: String): Boolean =
        isProtected(pkg) && LockCrypto.verify(secret,
            if (hasSeparate(pkg)) p.getString(itemKey(pkg), null)
            else p.getString("master", null))

    fun protect(pkg: String) {
        check(hasMaster()) { "Master credential needed first" }
        val packages = protectedPackages().toMutableSet()
        packages.add(pkg)
        p.edit().putStringSet("protected", packages).apply()
    }

    fun unprotect(pkg: String) {
        val packages = protectedPackages().toMutableSet()
        packages.remove(pkg)
        p.edit().putStringSet("protected", packages)
            .remove(itemKey(pkg))
            .remove(methodKey(pkg))
            .remove(failKey(pkg))
            .remove(untilKey(pkg))
            .apply()
    }

    fun setIndividual(pkg: String, kind: String, secret: String) {
        check(hasMaster()) { "Master credential needed first" }
        val record = LockCrypto.create(kind, secret)
        val packages = protectedPackages().toMutableSet()
        packages.add(pkg)
        p.edit().putStringSet("protected", packages)
            .putString(itemKey(pkg), record)
            .putString(methodKey(pkg), "individual")
            .remove(failKey(pkg))
            .remove(untilKey(pkg))
            .apply()
    }

    fun useMaster(pkg: String) {
        if (!hasMaster()) return
        p.edit().remove(itemKey(pkg)).remove(methodKey(pkg))
            .remove(failKey(pkg)).remove(untilKey(pkg)).apply()
    }

    fun lockoutRemainingMs(pkg: String, now: Long = System.currentTimeMillis()): Long =
        (p.getLong(untilKey(pkg), 0L) - now).coerceAtLeast(0)

    fun recordAttempt(pkg: String, successful: Boolean,
                      now: Long = System.currentTimeMillis()) {
        val edit = p.edit()
        if (successful) {
            edit.remove(failKey(pkg)).remove(untilKey(pkg))
        } else {
            val failures = p.getInt(failKey(pkg), 0) + 1
            val until = LockAttemptPolicy.nextUntil(failures, now)
            if (until > 0) {
                edit.putInt(failKey(pkg), 0).putLong(untilKey(pkg), until)
            } else edit.putInt(failKey(pkg), failures)
        }
        edit.apply()
    }
}
