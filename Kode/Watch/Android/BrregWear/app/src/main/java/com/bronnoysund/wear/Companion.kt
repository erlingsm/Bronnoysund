// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// MessageClient wrapper. Sends a LookupRequest to the paired phone-host
// and awaits a single reply on the same path.

package com.bronnoysund.wear

import android.content.Context
import com.google.android.gms.wearable.MessageClient
import com.google.android.gms.wearable.MessageEvent
import com.google.android.gms.wearable.Wearable
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.tasks.await
import kotlinx.serialization.json.Json
import kotlin.coroutines.resume

class Companion(context: Context) {

    private val messageClient: MessageClient = Wearable.getMessageClient(context)
    private val nodeClient = Wearable.getNodeClient(context)
    private val json = Json { ignoreUnknownKeys = true; encodeDefaults = true }

    suspend fun send(request: LookupRequest): LookupResponse {
        val nodes = nodeClient.connectedNodes.await()
        val phone = nodes.firstOrNull { it.isNearby }
            ?: return LookupResponse(
                version = 1,
                result = "unavailable",
                message = "Telefonen er ikke tilgjengelig.",
            )

        val payload = json.encodeToString(LookupRequest.serializer(), request).toByteArray()

        return suspendCancellableCoroutine { cont ->
            lateinit var listener: MessageClient.OnMessageReceivedListener
            val resumeOnce: (LookupResponse) -> Unit = { response ->
                if (cont.isActive) {
                    messageClient.removeListener(listener)
                    cont.resume(response)
                }
            }
            listener = MessageClient.OnMessageReceivedListener { event ->
                if (event.path == ProtocolPaths.LOOKUP) {
                    val parsed = runCatching {
                        json.decodeFromString(LookupResponse.serializer(), String(event.data))
                    }.getOrElse {
                        LookupResponse(
                            version = 1,
                            result = "unavailable",
                            message = "Ugyldig svar fra telefonen.",
                        )
                    }
                    resumeOnce(parsed)
                }
            }
            messageClient.addListener(listener)
            cont.invokeOnCancellation { messageClient.removeListener(listener) }

            messageClient.sendMessage(phone.id, ProtocolPaths.LOOKUP, payload)
                .addOnFailureListener { error ->
                    resumeOnce(
                        LookupResponse(
                            version = 1,
                            result = "unavailable",
                            message = error.localizedMessage ?: "Ukjent feil.",
                        ),
                    )
                }
        }
    }

    @Suppress("unused")
    private fun MessageEvent.unused() = Unit
}
