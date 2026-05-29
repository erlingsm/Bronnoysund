// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// Wire-format DTOs for the Watch <-> Phone companion protocol v1.
// Spec: Kode/docs/watch-protocol.md

import Foundation

enum LookupAction: String, Codable {
    case lookup
    case search
}

struct LookupRequest: Codable {
    let version: Int
    let action: LookupAction
    let value: String

    init(action: LookupAction, value: String) {
        self.version = 1
        self.action = action
        self.value = value
    }
}

enum LookupResult: String, Codable {
    case found
    case notFound
    case invalid
    case unavailable
}

struct LookupResponse: Codable {
    let version: Int
    let result: LookupResult

    let organizationNumber: String?
    let organizationName: String?
    let companyType: String?
    let languageForm: String?
    let countryCode: String?

    let value: String?
    let message: String?
    let code: String?
}

extension LookupResponse {
    static func decode(_ data: Data) throws -> LookupResponse {
        try JSONDecoder().decode(LookupResponse.self, from: data)
    }
}

extension LookupRequest {
    func encode() throws -> Data {
        try JSONEncoder().encode(self)
    }
}
