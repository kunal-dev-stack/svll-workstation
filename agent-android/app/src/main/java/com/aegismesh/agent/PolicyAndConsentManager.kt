package com.aegismesh.agent

import android.content.Context
import android.content.SharedPreferences

class PolicyAndConsentManager(context: Context) {

    private val prefs: SharedPreferences = context.getSharedPreferences("aegis_consent_prefs", Context.MODE_PRIVATE)

    companion object {
        private const val KEY_CONSENT_GRANTED = "consent_granted"
        private const val KEY_CONSENT_TIMESTAMP = "consent_timestamp"
        private const val KEY_ATTENDED_ONLY = "attended_only_mode"
        private const val KEY_SMS_LOGS_PERMITTED = "sms_logs_permitted"
        private const val KEY_CALL_LOGS_PERMITTED = "call_logs_permitted"
    }

    var isConsentGranted: Boolean
        get() = prefs.getBoolean(KEY_CONSENT_GRANTED, false)
        set(value) = prefs.edit().putBoolean(KEY_CONSENT_GRANTED, value).putLong(KEY_CONSENT_TIMESTAMP, System.currentTimeMillis()).apply()

    var isAttendedOnlyMode: Boolean
        get() = prefs.getBoolean(KEY_ATTENDED_ONLY, true)
        set(value) = prefs.edit().putBoolean(KEY_ATTENDED_ONLY, value).apply()

    var isSmsPermittedByPolicy: Boolean
        get() = prefs.getBoolean(KEY_SMS_LOGS_PERMITTED, false)
        set(value) = prefs.edit().putBoolean(KEY_SMS_LOGS_PERMITTED, value).apply()

    var isCallLogsPermittedByPolicy: Boolean
        get() = prefs.getBoolean(KEY_CALL_LOGS_PERMITTED, false)
        set(value) = prefs.edit().putBoolean(KEY_CALL_LOGS_PERMITTED, value).apply()

    fun revokeAllConsent() {
        prefs.edit().clear().apply()
    }
}
