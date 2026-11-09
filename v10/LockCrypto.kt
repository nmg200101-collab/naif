package com.smartscreen.control

import java.security.MessageDigest
import java.security.SecureRandom
import java.util.Base64
import javax.crypto.SecretKeyFactory
import javax.crypto.spec.PBEKeySpec

/** Non-reversible salted PBKDF2 credentials. No raw PIN/pattern/password is persisted. */
object LockCrypto {
    const val PIN = "pin"
    const val PASSWORD = "password"
    const val PATTERN = "pattern"
    private const val ITERATIONS = 180_000
    private const val KEY_BITS = 256

    fun isValid(kind: String, secret: String): Boolean = when (kind) {
        PIN -> secret.matches(Regex("[0-9]{4,12}"))
        PASSWORD -> secret.length in 6..64 && secret.none { it.isISOControl() }
        PATTERN -> secret.length in 4..9 &&
            secret.all { it in '1'..'9' } && secret.toSet().size == secret.length
        else -> false
    }

    private fun hash(secret: String, salt: ByteArray): ByteArray {
        val chars = secret.toCharArray()
        val spec = PBEKeySpec(chars, salt, ITERATIONS, KEY_BITS)
        return try {
            SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256").generateSecret(spec).encoded
        } finally {
            spec.clearPassword()
            chars.fill('\u0000')
        }
    }

    fun create(kind: String, secret: String): String {
        require(isValid(kind, secret)) { "Invalid credential format" }
        val salt = ByteArray(16).also { SecureRandom().nextBytes(it) }
        val digest = hash(secret, salt)
        val encoder = Base64.getUrlEncoder().withoutPadding()
        return listOf(kind, encoder.encodeToString(salt),
            encoder.encodeToString(digest)).joinToString(":")
    }

    fun kind(encoded: String?): String? {
        val first = encoded?.substringBefore(":")
        return if (first in setOf(PIN, PASSWORD, PATTERN)) first else null
    }

    fun verify(secret: String, encoded: String?): Boolean {
        if (encoded == null) return false
        val segments = encoded.split(":")
        if (segments.size != 3 || !isValid(segments[0], secret)) return false
        return try {
            val decoder = Base64.getUrlDecoder()
            val salt = decoder.decode(segments[1])
            val expected = decoder.decode(segments[2])
            if (salt.size != 16 || expected.size != 32) return false
            MessageDigest.isEqual(expected, hash(secret, salt))
        } catch (_: Exception) { false }
    }
}

object LockAttemptPolicy {
    const val MAX_FAILURES = 5
    const val PAUSE_MS = 30_000L
    fun locked(until: Long, now: Long) = now < until
    fun nextUntil(failures: Int, now: Long): Long =
        if (failures >= MAX_FAILURES) now + PAUSE_MS else 0L
}
