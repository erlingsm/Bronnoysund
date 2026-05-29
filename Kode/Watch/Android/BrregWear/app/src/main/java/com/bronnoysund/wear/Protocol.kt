// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// Wire-format DTOs for the Watch <-> Phone companion protocol v1.
// Spec: Kode/docs/watch-protocol.md

package com.bronnoysund.wear

import kotlinx.serialization.Serializable

@Serializable
data class LookupRequest(
    val version: Int = 1,
    val action: String,
    val value: String,
)

@Serializable
data class LookupResponse(
    val version: Int,
    val result: String,
    val organizationNumber: String? = null,
    val organizationName: String? = null,
    val companyType: String? = null,
    val languageForm: String? = null,
    val countryCode: String? = null,
    val value: String? = null,
    val message: String? = null,
    val code: String? = null,
)

object ProtocolPaths {
    const val LOOKUP = "/com.bronnoysund/lookup/v1"
}
