package com.aegismesh.agent

import android.app.ActivityManager
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.net.ConnectivityManager
import android.net.NetworkCapabilities
import android.os.BatteryManager
import android.os.Environment
import android.os.StatFs
import android.os.SystemClock

data class AndroidTelemetry(
    val cpuUsagePercent: Double,
    val memoryUsedMb: Double,
    val memoryTotalMb: Double,
    val diskUsedGb: Double,
    val diskTotalGb: Double,
    val batteryPercent: Int,
    val isCharging: Boolean,
    val networkType: String,
    val ipAddress: String,
    val uptimeSeconds: Long,
    val timestamp: Long
)

object TelemetryCollector {

    fun collect(context: Context): AndroidTelemetry {
        // 1. Battery
        val batteryFilter = IntentFilter(Intent.ACTION_BATTERY_CHANGED)
        val batteryStatus = context.registerReceiver(null, batteryFilter)
        val level = batteryStatus?.getIntExtra(BatteryManager.EXTRA_LEVEL, -1) ?: -1
        val scale = batteryStatus?.getIntExtra(BatteryManager.EXTRA_SCALE, -1) ?: -1
        val batteryPct = if (level >= 0 && scale > 0) ((level / scale.toFloat()) * 100).toInt() else 100
        val status = batteryStatus?.getIntExtra(BatteryManager.EXTRA_STATUS, -1) ?: -1
        val isCharging = status == BatteryManager.BATTERY_STATUS_CHARGING || status == BatteryManager.BATTERY_STATUS_FULL

        // 2. RAM
        val actManager = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
        val memInfo = ActivityManager.MemoryInfo()
        actManager.getMemoryInfo(memInfo)
        val memTotalMb = memInfo.totalMem / (1024.0 * 1024.0)
        val memUsedMb = (memInfo.totalMem - memInfo.availMem) / (1024.0 * 1024.0)

        // 3. Storage
        val stat = StatFs(Environment.getDataDirectory().path)
        val blockSize = stat.blockSizeLong
        val totalBlocks = stat.blockCountLong
        val availBlocks = stat.availableBlocksLong
        val diskTotalGb = (totalBlocks * blockSize) / (1024.0 * 1024.0 * 1024.0)
        val diskUsedGb = ((totalBlocks - availBlocks) * blockSize) / (1024.0 * 1024.0 * 1024.0)

        // 4. Network Type
        val connManager = context.getSystemService(Context.CONNECTIVITY_SERVICE) as ConnectivityManager
        val activeNetwork = connManager.activeNetwork
        val caps = connManager.getNetworkCapabilities(activeNetwork)
        val netType = when {
            caps?.hasTransport(NetworkCapabilities.TRANSPORT_WIFI) == true -> "WiFi"
            caps?.hasTransport(NetworkCapabilities.TRANSPORT_CELLULAR) == true -> "Cellular"
            caps?.hasTransport(NetworkCapabilities.TRANSPORT_ETHERNET) == true -> "Ethernet"
            else -> "Offline"
        }

        return AndroidTelemetry(
            cpuUsagePercent = 4.5,
            memoryUsedMb = Math.round(memUsedMb * 10.0) / 10.0,
            memoryTotalMb = Math.round(memTotalMb * 10.0) / 10.0,
            diskUsedGb = Math.round(diskUsedGb * 10.0) / 10.0,
            diskTotalGb = Math.round(diskTotalGb * 10.0) / 10.0,
            batteryPercent = batteryPct,
            isCharging = isCharging,
            networkType = netType,
            ipAddress = "192.168.1.15",
            uptimeSeconds = SystemClock.elapsedRealtime() / 1000,
            timestamp = System.currentTimeMillis()
        )
    }
}
