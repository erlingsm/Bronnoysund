// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// WCSession wrapper. Owns the single shared session and exposes a single
// async send(_:) call that returns the decoded response.

import Foundation
import WatchConnectivity

@MainActor
final class Companion: NSObject, ObservableObject, WCSessionDelegate {
    @Published var lastResponse: LookupResponse?
    @Published var lastError: String?
    @Published var isReachable: Bool = false

    private let session: WCSession?

    override init() {
        if WCSession.isSupported() {
            self.session = WCSession.default
        } else {
            self.session = nil
        }
        super.init()
        session?.delegate = self
        session?.activate()
        self.isReachable = session?.isReachable ?? false
    }

    func send(_ request: LookupRequest) async {
        guard let session, session.activationState == .activated else {
            self.lastError = "Telefonen er ikke tilgjengelig. Åpne appen på telefonen din."
            return
        }
        guard session.isReachable else {
            self.lastError = "Telefonen er ikke tilgjengelig. Åpne appen på telefonen din."
            return
        }
        do {
            let payload = try request.encode()
            try await withCheckedThrowingContinuation { (cont: CheckedContinuation<Void, Error>) in
                session.sendMessageData(payload, replyHandler: { reply in
                    Task { @MainActor in
                        do {
                            let decoded = try LookupResponse.decode(reply)
                            self.lastResponse = decoded
                            self.lastError = nil
                        } catch {
                            self.lastError = "Ugyldig svar fra telefonen."
                        }
                        cont.resume()
                    }
                }, errorHandler: { error in
                    Task { @MainActor in
                        self.lastError = error.localizedDescription
                        cont.resume()
                    }
                })
            }
        } catch {
            self.lastError = "Kunne ikke kode forespørsel: \(error.localizedDescription)"
        }
    }

    nonisolated func session(_ session: WCSession,
                             activationDidCompleteWith activationState: WCSessionActivationState,
                             error: Error?) {
        Task { @MainActor in
            self.isReachable = session.isReachable
        }
    }

    nonisolated func sessionReachabilityDidChange(_ session: WCSession) {
        Task { @MainActor in
            self.isReachable = session.isReachable
        }
    }
}
