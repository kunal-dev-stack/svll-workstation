package com.aegismesh.agent

import android.app.*
import android.content.Context
import android.content.Intent
import android.media.projection.MediaProjection
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.IBinder
import androidx.core.app.NotificationCompat

class AegisForegroundService : Service() {

    private var socketClient: MeshSocketClient? = null
    private var screenCaptureService: ScreenCaptureService? = null
    private var mediaProjection: MediaProjection? = null

    companion object {
        const val CHANNEL_ID = "AegisMeshServiceChannel"
        const val NOTIFICATION_ID = 1001

        const val ACTION_START = "ACTION_START"
        const val ACTION_STOP = "ACTION_STOP"
        const val ACTION_START_SCREEN = "ACTION_START_SCREEN"

        const val EXTRA_SERVER_URL = "EXTRA_SERVER_URL"
        const val EXTRA_TOKEN = "EXTRA_TOKEN"
        const val EXTRA_DEVICE_ID = "EXTRA_DEVICE_ID"
        const val EXTRA_PROJECTION_DATA = "EXTRA_PROJECTION_DATA"
    }

    override fun onCreate() {
        super.onCreate()
        createNotificationChannel()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        when (intent?.action) {
            ACTION_START -> {
                val serverUrl = intent.getStringExtra(EXTRA_SERVER_URL) ?: "ws://10.0.2.2:5000"
                val token = intent.getStringExtra(EXTRA_TOKEN) ?: "MESH-ENTERPRISE-TOKEN-2026"
                val deviceId = intent.getStringExtra(EXTRA_DEVICE_ID) ?: "and-${Build.MODEL.lowercase()}"

                startForeground(NOTIFICATION_ID, buildNotification("Connected to AegisMesh Gateway"))
                initializeSocket(serverUrl, token, deviceId)
            }

            ACTION_START_SCREEN -> {
                val projIntent = intent.getParcelableExtra<Intent>(EXTRA_PROJECTION_DATA)
                if (projIntent != null) {
                    val projectionManager = getSystemService(Context.MEDIA_PROJECTION_SERVICE) as MediaProjectionManager
                    mediaProjection = projectionManager.getMediaProjection(Activity.RESULT_OK, projIntent)
                    mediaProjection?.let { mp ->
                        screenCaptureService = ScreenCaptureService(mp) { jpegBytes ->
                            socketClient?.sendBinaryFrame(jpegBytes)
                        }
                        screenCaptureService?.start()
                        updateNotification("Screen Sharing Active: Authorized Remote Session")
                    }
                }
            }

            ACTION_STOP -> {
                screenCaptureService?.stop()
                socketClient?.disconnect()
                stopForeground(STOP_FOREGROUND_REMOVE)
                stopSelf()
            }
        }
        return START_STICKY
    }

    private fun initializeSocket(serverUrl: String, token: String, deviceId: String) {
        socketClient = MeshSocketClient(
            serverUrl = serverUrl,
            token = token,
            deviceId = deviceId,
            onMessageReceived = { type, payload ->
                when (type) {
                    "SESSION_DISCONNECT" -> {
                        screenCaptureService?.stop()
                        updateNotification("Connected: Standby Mode")
                    }
                }
            },
            onConnectionChanged = { connected ->
                if (connected) {
                    updateNotification("Connected to IT Gateway (Online)")
                    val telemetry = TelemetryCollector.collect(this)
                    socketClient?.sendHeartbeat(telemetry)
                } else {
                    updateNotification("Gateway Disconnected (Reconnecting...)")
                }
            }
        )
        socketClient?.connect()
    }

    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                CHANNEL_ID,
                "AegisMesh Remote Management Service",
                NotificationManager.IMPORTANCE_LOW
            ).apply {
                description = "Shows persistent status during authorized enterprise remote support sessions"
            }
            val manager = getSystemService(NotificationManager::class.java)
            manager?.createNotificationChannel(channel)
        }
    }

    private fun buildNotification(statusText: String): Notification {
        val stopIntent = Intent(this, AegisForegroundService::class.java).apply {
            action = ACTION_STOP
        }
        val stopPendingIntent = PendingIntent.getService(
            this,
            0,
            stopIntent,
            PendingIntent.FLAG_IMMUTABLE or PendingIntent.FLAG_UPDATE_CURRENT
        )

        return NotificationCompat.Builder(this, CHANNEL_ID)
            .setContentTitle("AegisMesh Endpoint Agent")
            .setContentText(statusText)
            .setSmallIcon(android.R.drawable.ic_dialog_info)
            .setOngoing(true)
            .addAction(android.R.drawable.ic_menu_close_clear_cancel, "Disconnect", stopPendingIntent)
            .build()
    }

    private fun updateNotification(statusText: String) {
        val manager = getSystemService(NotificationManager::class.java)
        manager?.notify(NOTIFICATION_ID, buildNotification(statusText))
    }

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onDestroy() {
        screenCaptureService?.stop()
        socketClient?.disconnect()
        super.onDestroy()
    }
}
