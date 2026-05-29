// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

package com.bronnoysund.wear

import android.Manifest
import android.content.pm.PackageManager
import android.os.Bundle
import android.speech.tts.TextToSpeech
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.lifecycle.lifecycleScope
import androidx.wear.compose.foundation.lazy.ScalingLazyColumn
import androidx.wear.compose.material.Button
import androidx.wear.compose.material.MaterialTheme
import androidx.wear.compose.material.Scaffold
import androidx.wear.compose.material.Text
import com.bronnoysund.wear.ui.CompanyDataRow
import kotlinx.coroutines.launch
import java.util.Locale

class MainActivity : ComponentActivity() {

    private lateinit var companion: Companion
    private lateinit var speech: SpeechCapture
    private var tts: TextToSpeech? = null

    private val micPermission =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) { /* state observed by UI */ }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        companion = Companion(applicationContext)
        speech = SpeechCapture(applicationContext)
        tts = TextToSpeech(applicationContext) { status ->
            if (status == TextToSpeech.SUCCESS) tts?.language = Locale("nb", "NO")
        }

        if (checkSelfPermission(Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
            micPermission.launch(Manifest.permission.RECORD_AUDIO)
        }

        setContent {
            MaterialTheme {
                LookupScreen(
                    onSpeak = { onSpeak() },
                    onReadAloud = { name, type -> tts?.speak("$name, $type", TextToSpeech.QUEUE_FLUSH, null, "result") },
                    response = response,
                    lastInput = lastInput,
                    isWorking = isWorking,
                )
            }
        }
    }

    override fun onDestroy() {
        tts?.shutdown()
        super.onDestroy()
    }

    private var response by mutableStateOf<LookupResponse?>(null)
    private var lastInput by mutableStateOf("")
    private var isWorking by mutableStateOf(false)

    private fun onSpeak() {
        lifecycleScope.launch {
            try {
                isWorking = true
                val value = speech.captureOnce()
                lastInput = value
                if (value.isNotEmpty()) {
                    response = companion.send(LookupRequest(action = "lookup", value = value))
                }
            } catch (e: Exception) {
                response = LookupResponse(version = 1, result = "unavailable", message = e.localizedMessage)
            } finally {
                isWorking = false
            }
        }
    }
}
