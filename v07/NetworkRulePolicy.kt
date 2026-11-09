package com.smartscreen.control

/**
 * Pure network rules, usable in unit tests without an Android emulator.
 * "BACKGROUND" intentionally fails open when we cannot reliably identify the
 * foreground application. Full deny rules still apply.
 */
object NetworkRulePolicy {
    const val ALLOW = "allow"
    const val BACKGROUND = "background"
    const val DENY = "deny"

    fun normalize(value: String?): String = when (value) {
        ALLOW -> ALLOW
        BACKGROUND -> BACKGROUND
        DENY -> DENY
        else -> ALLOW
    }

    fun isBlocked(
        mode: String?,
        packageName: String,
        foregroundPackage: String?,
        foregroundReliable: Boolean
    ): Boolean = when (normalize(mode)) {
        DENY -> true
        BACKGROUND -> foregroundReliable && foregroundPackage != packageName
        else -> false
    }

    fun needsEnforcement(mode: String?): Boolean =
        normalize(mode) == DENY || normalize(mode) == BACKGROUND
}
