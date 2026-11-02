package com.aegismesh.agent

import android.app.Activity
import android.content.Context
import android.content.Intent
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.Bundle
import android.widget.Button
import android.widget.EditText
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity

class MainActivity : AppCompatActivity() {

    private lateinit var consentManager: PolicyAndConsentManager
    private var isConnected = false

    private val screenCaptureLauncher = registerForActivityResult(
        ActivityResultContracts.StartActivityForResult()
    ) { result ->
        if (result.resultCode == Activity.RESULT_OK && result.data != null) {
            val serviceIntent = Intent(this, AegisForegroundService::class.java).apply {
                action = AegisForegroundService.ACTION_START_SCREEN
                putExtra(AegisForegroundService.EXTRA_PROJECTION_DATA, result.data)
            }
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                startForegroundService(serviceIntent)
            } else {
                startService(serviceIntent)
            }
            Toast.makeText(this, "Remote screen streaming activated", Toast.LENGTH_SHORT).show()
        } else {
            Toast.makeText(this, "Screen capture permission declined", Toast.LENGTH_SHORT).show()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        consentManager = PolicyAndConsentManager(this)

        // Simple layout construction for maximum robustness
        val layout = android.widget.LinearLayout(this).apply {
            orientation = android.widget.LinearLayout.VERTICAL
            setPadding(40, 60, 40, 40)
            setBackgroundColor(0xFF0F172A.toInt())
        }

        val title = TextView(this).apply {
            text = "🛡️ AegisMesh Android Endpoint Agent"
            textSize = 20f
            setTextColor(0xFFFFFFFF.toInt())
            setPadding(0, 0, 0, 20)
        }
        layout.addView(title)

        val desc = TextView(this).apply {
            text = "Authorized Enterprise Management & Screen Support Client"
            textSize = 14f
            setTextColor(0xFF94A3B8.toInt())
            setPadding(0, 0, 0, 30)
        }
        layout.addView(desc)

        val serverLabel = TextView(this).apply {
            text = "Mesh Server URL:"
            setTextColor(0xFFCBD5E1.toInt())
        }
        layout.addView(serverLabel)

        val editServer = EditText(this).apply {
            setText("ws://10.0.2.2:5000")
            setTextColor(0xFFFFFFFF.toInt())
            setBackgroundColor(0xFF1E293B.toInt())
            setPadding(20, 20, 20, 20)
        }
        layout.addView(editServer)

        val tokenLabel = TextView(this).apply {
            text = "Enrollment Token:"
            setTextColor(0xFFCBD5E1.toInt())
            setPadding(0, 20, 0, 0)
        }
        layout.addView(tokenLabel)

        val editToken = EditText(this).apply {
            setText("MESH-ENTERPRISE-TOKEN-2026")
            setTextColor(0xFFFFFFFF.toInt())
            setBackgroundColor(0xFF1E293B.toInt())
            setPadding(20, 20, 20, 20)
        }
        layout.addView(editToken)

        val btnToggle = Button(this).apply {
            text = "Connect to Gateway"
            setBackgroundColor(0xFF6366F1.toInt())
            setTextColor(0xFFFFFFFF.toInt())
            setPadding(0, 30, 0, 30)
        }

        val btnScreen = Button(this).apply {
            text = "Grant Screen Sharing Session"
            setBackgroundColor(0xFF06B6D4.toInt())
            setTextColor(0xFFFFFFFF.toInt())
            setPadding(0, 30, 0, 30)
            isEnabled = false
        }

        btnToggle.setOnClickListener {
            if (!isConnected) {
                val serviceIntent = Intent(this, AegisForegroundService::class.java).apply {
                    action = AegisForegroundService.ACTION_START
                    putExtra(AegisForegroundService.EXTRA_SERVER_URL, editServer.text.toString())
                    putExtra(AegisForegroundService.EXTRA_TOKEN, editToken.text.toString())
                    putExtra(AegisForegroundService.EXTRA_DEVICE_ID, "and-${Build.MODEL.lowercase().replace(" ", "-")}")
                }
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    startForegroundService(serviceIntent)
                } else {
                    startService(serviceIntent)
                }
                isConnected = true
                btnToggle.text = "Disconnect from Gateway"
                btnToggle.setBackgroundColor(0xFFF43F5E.toInt())
                btnScreen.isEnabled = true
                Toast.makeText(this, "AegisMesh Agent Connected", Toast.LENGTH_SHORT).show()
            } else {
                val serviceIntent = Intent(this, AegisForegroundService::class.java).apply {
                    action = AegisForegroundService.ACTION_STOP
                }
                startService(serviceIntent)
                isConnected = false
                btnToggle.text = "Connect to Gateway"
                btnToggle.setBackgroundColor(0xFF6366F1.toInt())
                btnScreen.isEnabled = false
            }
        }

        btnScreen.setOnClickListener {
            val projectionManager = getSystemService(Context.MEDIA_PROJECTION_SERVICE) as MediaProjectionManager
            screenCaptureLauncher.launch(projectionManager.createScreenCaptureIntent())
        }

        val spacer = android.view.View(this).apply {
            layoutParams = android.widget.LinearLayout.LayoutParams(1, 40)
        }
        layout.addView(spacer)
        layout.addView(btnToggle)

        val spacer2 = android.view.View(this).apply {
            layoutParams = android.widget.LinearLayout.LayoutParams(1, 20)
        }
        layout.addView(spacer2)
        layout.addView(btnScreen)

        setContentView(layout)
    }
}
