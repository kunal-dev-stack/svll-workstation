package com.aegismesh.agent

import android.os.Build
import android.util.Log
import com.google.gson.Gson
import com.google.gson.JsonObject
import okhttp3.*
import okio.ByteString
import okio.ByteString.Companion.toByteString
import java.util.concurrent.TimeUnit

class MeshSocketClient(
    private val serverUrl: String,
    private val token: String,
    private val deviceId: String,
    private val onMessageReceived: (type: String, payload: JsonObject) -> Unit,
    private val onConnectionChanged: (connected: Boolean) -> Unit
) {
    private val client = OkHttpClient.Builder()
        .readTimeout(0, TimeUnit.MILLISECONDS)
        .pingInterval(15, TimeUnit.SECONDS)
        .build()

    private var webSocket: WebSocket? = null
    private val gson = Gson()
    private val TAG = "AegisMeshSocket"

    fun connect() {
        val cleanUrl = serverUrl.removeSuffix("/")
        val wsUrl = "$cleanUrl/ws?role=agent&deviceId=$deviceId&token=$token"

        val request = Request.Builder()
            .url(wsUrl)
            .build()

        webSocket = client.newWebSocket(request, object : WebSocketListener() {
            override fun onOpen(webSocket: WebSocket, response: Response) {
                Log.i(TAG, "Connected securely to AegisMesh Gateway")
                onConnectionChanged(true)
                sendRegistration()
            }

            override fun onMessage(webSocket: WebSocket, text: String) {
                try {
                    val json = gson.fromJson(text, JsonObject::class.java)
                    val type = json.get("type")?.asString ?: return
                    val payload = json.getAsJsonObject("payload") ?: JsonObject()
                    onMessageReceived(type, payload)
                } catch (e: Exception) {
                    Log.e(TAG, "Error parsing message: ${e.message}")
                }
            }

            override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
                Log.w(TAG, "Gateway closing: $reason")
                onConnectionChanged(false)
            }

            override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
                Log.e(TAG, "Connection failure: ${t.message}")
                onConnectionChanged(false)
            }
        })
    }

    fun disconnect() {
        webSocket?.close(1000, "User disconnected")
        webSocket = null
    }

    private fun sendRegistration() {
        val payload = mapOf(
            "hostname" to "${Build.MANUFACTURER} ${Build.MODEL}",
            "os" to "android",
            "osVersion" to Build.VERSION.RELEASE,
            "arch" to Build.SUPPORTED_ABIS.firstOrNull() ?: "arm64-v8a",
            "agentVersion" to "1.0.0",
            "capabilities" to mapOf(
                "remoteDesktop" to true,
                "remoteTerminal" to false,
                "fileTransfer" to true,
                "processManager" to false,
                "serviceManager" to false,
                "diagnostics" to true
            )
        )
        sendMessage("DEVICE_REGISTER", payload)
    }

    fun sendHeartbeat(telemetry: AndroidTelemetry) {
        sendMessage("DEVICE_HEARTBEAT", mapOf("telemetry" to telemetry))
    }

    fun sendBinaryFrame(bytes: ByteArray) {
        webSocket?.send(bytes.toByteString())
    }

    fun sendMessage(type: String, payload: Any) {
        val message = mapOf(
            "type" to type,
            "deviceId" to deviceId,
            "payload" to payload,
            "timestamp" to System.currentTimeMillis()
        )
        val json = gson.toJson(message)
        webSocket?.send(json)
    }
}
