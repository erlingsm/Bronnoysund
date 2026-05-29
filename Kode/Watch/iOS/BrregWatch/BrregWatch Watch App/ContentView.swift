// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

import SwiftUI
import AVFoundation

struct ContentView: View {
    @EnvironmentObject var companion: Companion
    @StateObject private var speech = SpeechRecognizer()
    @State private var input: String = ""
    @State private var speechAuthorized: Bool = false
    private let synthesizer = AVSpeechSynthesizer()

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 12) {
                voiceButton
                if !input.isEmpty {
                    Text("Søk: \(input)")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
                if let response = companion.lastResponse {
                    resultView(response)
                } else if let error = companion.lastError {
                    Text(error)
                        .foregroundColor(.red)
                        .font(.caption)
                } else if !companion.isReachable {
                    Text("Telefonen er ikke tilgjengelig.")
                        .foregroundColor(.secondary)
                        .font(.caption)
                }
            }
            .padding()
        }
        .task {
            speechAuthorized = await speech.requestAuthorization()
        }
    }

    private var voiceButton: some View {
        Button(action: toggleRecording) {
            HStack {
                Image(systemName: speech.isRecording ? "stop.circle.fill" : "mic.circle.fill")
                Text(speech.isRecording ? "Stopp" : "Snakk")
            }
            .frame(maxWidth: .infinity)
        }
        .disabled(!speechAuthorized)
    }

    private func toggleRecording() {
        if speech.isRecording {
            speech.stop()
            input = speech.currentValue()
            guard !input.isEmpty else { return }
            Task {
                await companion.send(LookupRequest(action: .lookup, value: input))
            }
        } else {
            do {
                try speech.start()
            } catch {
                companion.lastError = error.localizedDescription
            }
        }
    }

    @ViewBuilder
    private func resultView(_ response: LookupResponse) -> some View {
        switch response.result {
        case .found:
            VStack(alignment: .leading, spacing: 8) {
                CompanyDataRow(label: "Organisasjonsnummer", value: response.organizationNumber ?? "—")
                CompanyDataRow(label: "Navn", value: response.organizationName ?? "—")
                CompanyDataRow(label: "Selskapsform", value: response.companyType ?? "—")
                CompanyDataRow(label: "Målform", value: response.languageForm ?? "—")
                Button(action: { readAloud(response) }) {
                    HStack {
                        Image(systemName: "speaker.wave.2.fill")
                        Text("Les opp")
                    }
                    .frame(maxWidth: .infinity)
                }
                .padding(.top, 8)
            }
        case .notFound:
            Text(response.message ?? "Ingen treff.")
                .foregroundColor(.secondary)
        case .invalid:
            Text(response.message ?? "Ugyldig input.")
                .foregroundColor(.orange)
        case .unavailable:
            Text(response.message ?? "Tjenesten er utilgjengelig.")
                .foregroundColor(.red)
        }
    }

    private func readAloud(_ response: LookupResponse) {
        let name = response.organizationName ?? ""
        let type = response.companyType ?? ""
        let utterance = AVSpeechUtterance(string: "\(name), \(type)")
        utterance.voice = AVSpeechSynthesisVoice(language: "nb-NO")
        synthesizer.speak(utterance)
    }
}
