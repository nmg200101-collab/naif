package com.smartscreen.control

import org.junit.Assert.*
import org.junit.Test

class DailyQuotaPolicyTest {
    @Test fun noLimitNeverBlocks() {
        assertFalse(DailyQuotaPolicy.reached(0, Long.MAX_VALUE))
        assertFalse(DailyQuotaPolicy.activeBlock(0, 5, 5))
    }
    @Test fun belowLimitAllowed() =
        assertFalse(DailyQuotaPolicy.reached(100, 99L * DailyQuotaPolicy.MEBIBYTE))
    @Test fun limitIsInclusive() =
        assertTrue(DailyQuotaPolicy.reached(100, 100L * DailyQuotaPolicy.MEBIBYTE))
    @Test fun yesterdayDoesNotBlockToday() =
        assertFalse(DailyQuotaPolicy.activeBlock(100, 200, 201))
    @Test fun blockedUntilNextDay() =
        assertTrue(DailyQuotaPolicy.activeBlock(250, 400, 400))
    @Test fun unsupportedPresetFailsOpen() =
        assertEquals(0, DailyQuotaPolicy.normalizeMb(17))
    @Test fun yearlyResetSafe() {
        assertTrue(DailyQuotaPolicy.dayCode(2027, 1) >
            DailyQuotaPolicy.dayCode(2026, 365))
    }
}
