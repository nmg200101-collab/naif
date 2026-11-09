package com.smartscreen.control

import org.junit.Assert.*
import org.junit.Test

class LockCryptoTest {
    @Test fun pinMustHaveFourToTwelveDigits() {
        assertFalse(LockCrypto.isValid(LockCrypto.PIN, "12"))
        assertTrue(LockCrypto.isValid(LockCrypto.PIN, "012345"))
        assertFalse(LockCrypto.isValid(LockCrypto.PIN, "555a"))
    }
    @Test fun patternRequiresDistinctPoints() {
        assertTrue(LockCrypto.isValid(LockCrypto.PATTERN, "14789"))
        assertFalse(LockCrypto.isValid(LockCrypto.PATTERN, "1112"))
        assertFalse(LockCrypto.isValid(LockCrypto.PATTERN, "123"))
    }
    @Test fun passwordValidation() {
        assertTrue(LockCrypto.isValid(LockCrypto.PASSWORD, "strong-pass!"))
        assertFalse(LockCrypto.isValid(LockCrypto.PASSWORD, "1234"))
    }
    @Test fun saltedCredentialDoesNotExposeCode() {
        val record = LockCrypto.create(LockCrypto.PIN, "364815")
        assertFalse(record.contains("364815"))
        assertEquals(LockCrypto.PIN, LockCrypto.kind(record))
        assertTrue(LockCrypto.verify("364815", record))
        assertFalse(LockCrypto.verify("364816", record))
        assertFalse(LockCrypto.verify("364815", record.replace(":", "")))
    }
    @Test fun uniqueSaltsPerCredential() {
        val one = LockCrypto.create(LockCrypto.PATTERN, "14789")
        val two = LockCrypto.create(LockCrypto.PATTERN, "14789")
        assertNotEquals(one, two)
        assertTrue(LockCrypto.verify("14789", one))
        assertTrue(LockCrypto.verify("14789", two))
    }
    @Test fun rateLimitsFiveFailures() {
        assertEquals(0L, LockAttemptPolicy.nextUntil(4, 100L))
        assertEquals(30100L, LockAttemptPolicy.nextUntil(5, 100L))
        assertTrue(LockAttemptPolicy.locked(30100L, 30100L - 1))
        assertFalse(LockAttemptPolicy.locked(30100L, 30100L))
    }
}
