package com.smartscreen.control

/**
 * Pure quota policy for a soft daily data budget, not a bandwidth/speed limit.
 * Used only for totals Android makes available, so readings can be delayed.
 */
object DailyQuotaPolicy {
    const val MEBIBYTE = 1_048_576L
    val presetMb = intArrayOf(0, 50, 100, 250, 500, 1024)

    fun normalizeMb(value: Int): Int =
        if (value in presetMb) value else 0

    fun reached(limitMb: Int, usedBytes: Long): Boolean =
        limitMb > 0 && usedBytes >= limitMb.toLong() * MEBIBYTE

    fun activeBlock(limitMb: Int, lastReachedDay: Int, today: Int): Boolean =
        limitMb > 0 && lastReachedDay == today

    fun dayCode(year: Int, dayOfYear: Int): Int = year * 400 + dayOfYear
}
