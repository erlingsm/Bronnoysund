// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// watchOS does not expose Speech/SFSpeechRecognizer. We use the native
// presentTextInputController, which surfaces the system dictation flow
// (microphone first, scribble + emoji as secondary inputs). The captured
// string is fed straight into the companion message; tale-til-tall-
// normalisering kjører på telefon-siden via ICountryDetector.

import Foundation
import WatchKit

@MainActor
final class SpeechRecognizer: ObservableObject {
    @Published var transcript: String = ""
    @Published var isRecording: Bool = false
    @Published var error: String?

    func capture(completion: @escaping (String) -> Void) {
        guard let controller = WKApplication.shared().rootInterfaceController else {
            self.error = "Kunne ikke åpne taleinput."
            completion("")
            return
        }
        isRecording = true
        controller.presentTextInputController(
            withSuggestions: nil,
            allowedInputMode: .plain
        ) { [weak self] results in
            Task { @MainActor in
                guard let self else { return }
                self.isRecording = false
                let text = (results?.first as? String)?
                    .trimmingCharacters(in: .whitespacesAndNewlines) ?? ""
                self.transcript = text
                completion(text)
            }
        }
    }
}
