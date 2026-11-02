package com.aegismesh.agent

import android.graphics.Bitmap
import android.graphics.PixelFormat
import android.hardware.display.DisplayManager
import android.hardware.display.VirtualDisplay
import android.media.ImageReader
import android.media.projection.MediaProjection
import android.os.Handler
import android.os.Looper
import android.util.Log
import java.io.ByteArrayOutputStream

class ScreenCaptureService(
    private val mediaProjection: MediaProjection,
    private val width: Int = 720,
    private val height: Int = 1280,
    private val dpi: Int = 320,
    private val onFrameCaptured: (jpegBytes: ByteArray) -> Unit
) {
    private var virtualDisplay: VirtualDisplay? = null
    private var imageReader: ImageReader? = null
    private var isStreaming = false
    private val handler = Handler(Looper.getMainLooper())
    private val TAG = "ScreenCapture"

    fun start() {
        if (isStreaming) return
        isStreaming = true

        imageReader = ImageReader.newInstance(width, height, PixelFormat.RGBA_8888, 2)
        virtualDisplay = mediaProjection.createVirtualDisplay(
            "AegisMeshScreen",
            width,
            height,
            dpi,
            DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,
            imageReader?.surface,
            null,
            handler
        )

        imageReader?.setOnImageAvailableListener({ reader ->
            if (!isStreaming) return@setOnImageAvailableListener
            val image = reader.acquireLatestImage() ?: return@setOnImageAvailableListener

            try {
                val planes = image.planes
                val buffer = planes[0].buffer
                val pixelStride = planes[0].pixelStride
                val rowStride = planes[0].rowStride
                val rowPadding = rowStride - pixelStride * width

                val bitmap = Bitmap.createBitmap(
                    width + rowPadding / pixelStride,
                    height,
                    Bitmap.Config.ARGB_8888
                )
                bitmap.copyPixelsFromBuffer(buffer)

                // Compress to JPEG
                val out = ByteArrayOutputStream()
                bitmap.compress(Bitmap.CompressFormat.JPEG, 60, out)
                val bytes = out.toByteArray()
                bitmap.recycle()

                onFrameCaptured(bytes)
            } catch (e: Exception) {
                Log.e(TAG, "Error encoding frame: ${e.message}")
            } finally {
                image.close()
            }
        }, handler)

        Log.i(TAG, "Screen streaming virtual display initiated")
    }

    fun stop() {
        isStreaming = false
        virtualDisplay?.release()
        virtualDisplay = null
        imageReader?.close()
        imageReader = null
        Log.i(TAG, "Screen streaming stopped")
    }
}
