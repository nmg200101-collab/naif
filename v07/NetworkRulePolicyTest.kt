package com.smartscreen.control

import org.junit.Assert.*
import org.junit.Test

class NetworkRulePolicyTest {
    private val pkg = "org.example.reader"
    @Test fun allowedWorksInAnyState() {
        assertFalse(NetworkRulePolicy.isBlocked("allow", pkg, null, true))
        assertFalse(NetworkRulePolicy.isBlocked("allow", pkg, "other", true))
    }
    @Test fun fullBlockIsUnconditional() {
        assertTrue(NetworkRulePolicy.isBlocked("deny", pkg, pkg, true))
        assertTrue(NetworkRulePolicy.isBlocked("deny", pkg, null, false))
    }
    @Test fun foregroundAllowsAccess() =
        assertFalse(NetworkRulePolicy.isBlocked("background", pkg, pkg, true))
    @Test fun backgroundBlocksOtherApps() =
        assertTrue(NetworkRulePolicy.isBlocked("background", pkg, "other", true))
    @Test fun screenLockedBlocksBackground() =
        assertTrue(NetworkRulePolicy.isBlocked("background", pkg, null, true))
    @Test fun uncertainForegroundDoesNotSilentlyBlock() =
        assertFalse(NetworkRulePolicy.isBlocked("background", pkg, null, false))
    @Test fun unknownModeDefaultsToAllow() =
        assertEquals("allow", NetworkRulePolicy.normalize("bad"))
    @Test fun backgroundRequiresEnforcement() =
        assertTrue(NetworkRulePolicy.needsEnforcement("background"))
}
