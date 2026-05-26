// SPDX-License-Identifier: AGPL-3.0-or-later OR LicenseRef-Commercial
//
// Web Speech API shim. Two functions:
//
//   bronnoysundSpeech.listen(languageCode)   -> Promise<string|null>
//   bronnoysundSpeech.speak(text, languageCode) -> Promise<void>
//
// All capture and synthesis happens in the browser. No data leaves the user's machine.
//
// Compatibility:
//   - Chrome / Edge desktop: full STT + TTS support
//   - Safari desktop / iOS: speechSynthesis works; SpeechRecognition unsupported, listen() returns null
//   - Firefox: TTS works, STT effectively unsupported, listen() returns null
//
// Listen() resolves with the final transcript (no interim partials surfaced — Plan 04b leaves
// streaming partials to a later iteration). Returns null on permission denial, no-speech
// timeout, or unsupported browser.

window.bronnoysundSpeech = (function () {
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    function listen(languageCode) {
        return new Promise(function (resolve) {
            if (!Recognition) {
                resolve(null);
                return;
            }

            const rec = new Recognition();
            rec.lang = languageCode || 'nb-NO';
            rec.continuous = false;
            rec.interimResults = false;
            rec.maxAlternatives = 1;

            let settled = false;
            const finish = function (value) {
                if (settled) return;
                settled = true;
                try { rec.stop(); } catch (e) { /* already stopped */ }
                resolve(value);
            };

            rec.onresult = function (event) {
                const result = event.results[0];
                if (result && result[0]) {
                    finish(result[0].transcript);
                } else {
                    finish(null);
                }
            };
            rec.onerror = function () { finish(null); };
            rec.onnomatch = function () { finish(null); };
            rec.onend = function () { finish(null); };

            try {
                rec.start();
            } catch (e) {
                finish(null);
            }
        });
    }

    function speak(text, languageCode) {
        return new Promise(function (resolve) {
            if (!window.speechSynthesis || !text) {
                resolve();
                return;
            }

            const utter = new SpeechSynthesisUtterance(text);
            utter.lang = languageCode || 'nb-NO';
            utter.rate = 1.0;
            utter.pitch = 1.0;
            utter.onend = function () { resolve(); };
            utter.onerror = function () { resolve(); };

            // Pick the first installed voice that matches the requested locale. If none does,
            // the browser falls back to its default voice — usually still intelligible in
            // English, just not Norwegian.
            const voices = window.speechSynthesis.getVoices();
            const match = voices.find(function (v) { return v.lang === languageCode; })
                ?? voices.find(function (v) { return v.lang.startsWith(languageCode.substring(0, 2)); });
            if (match) {
                utter.voice = match;
            }

            window.speechSynthesis.cancel();
            window.speechSynthesis.speak(utter);
        });
    }

    return { listen: listen, speak: speak };
})();
