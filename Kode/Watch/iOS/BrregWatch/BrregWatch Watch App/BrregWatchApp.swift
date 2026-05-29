// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial

import SwiftUI

@main
struct BrregWatchApp: App {
    @StateObject private var companion = Companion()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(companion)
        }
    }
}
