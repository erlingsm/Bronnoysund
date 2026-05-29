// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

package com.bronnoysund.wear

import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import androidx.wear.compose.foundation.lazy.ScalingLazyColumn
import androidx.wear.compose.material.Button
import androidx.wear.compose.material.MaterialTheme
import androidx.wear.compose.material.Text
import com.bronnoysund.wear.ui.CompanyDataRow

@Composable
fun LookupScreen(
    onSpeak: () -> Unit,
    onReadAloud: (name: String, type: String) -> Unit,
    response: LookupResponse?,
    lastInput: String,
    isWorking: Boolean,
) {
    ScalingLazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(horizontal = 12.dp, vertical = 24.dp),
    ) {
        item {
            Button(
                onClick = onSpeak,
                enabled = !isWorking,
                modifier = Modifier.fillMaxWidth(),
            ) {
                Text(stringResource(if (isWorking) R.string.stop else R.string.speak))
            }
        }
        if (lastInput.isNotEmpty()) {
            item {
                Text(
                    text = "Søk: $lastInput",
                    style = MaterialTheme.typography.caption1,
                    color = MaterialTheme.colors.onSurfaceVariant,
                )
            }
        }
        when (response?.result) {
            "found" -> {
                item { CompanyDataRow(stringResource(R.string.org_number), response.organizationNumber.orEmpty()) }
                item { CompanyDataRow(stringResource(R.string.org_name), response.organizationName.orEmpty()) }
                item { CompanyDataRow(stringResource(R.string.company_type), response.companyType.orEmpty()) }
                item { CompanyDataRow(stringResource(R.string.language_form), response.languageForm.orEmpty()) }
                item {
                    Button(
                        onClick = {
                            onReadAloud(
                                response.organizationName.orEmpty(),
                                response.companyType.orEmpty(),
                            )
                        },
                        modifier = Modifier.fillMaxWidth(),
                    ) {
                        Text(stringResource(R.string.read_aloud))
                    }
                }
            }
            "notFound", "invalid", "unavailable" -> {
                item {
                    Text(
                        text = response?.message.orEmpty().ifEmpty { stringResource(R.string.phone_unreachable) },
                        color = MaterialTheme.colors.onSurfaceVariant,
                    )
                }
            }
            else -> {
                item {
                    Text(
                        text = stringResource(R.string.phone_unreachable),
                        color = MaterialTheme.colors.onSurfaceVariant,
                    )
                }
            }
        }
    }
}
