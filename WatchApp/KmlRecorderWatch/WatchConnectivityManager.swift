import Foundation
import WatchConnectivity

class WatchConnectivityManager: NSObject, ObservableObject, WCSessionDelegate {
    @Published var isRecording = false
    @Published var checkedInDate: Date? = nil
    @Published var isReachable = false

    private var session: WCSession?

    override init() {
        super.init()
        if WCSession.isSupported() {
            session = WCSession.default
            session?.delegate = self
            session?.activate()
        }
    }

    // MARK: - Actions

    func toggleTracking() {
        guard let session = session, session.isReachable else { return }
        session.sendMessage(["action": "toggle"], replyHandler: { reply in
            DispatchQueue.main.async {
                self.updateState(from: reply)
            }
        }, errorHandler: { error in
            print("Watch send error: \(error.localizedDescription)")
        })
    }

    // MARK: - State

    private func updateState(from message: [String: Any]) {
        if let recording = message["isRecording"] as? Bool {
            self.isRecording = recording
        }
        if let dateString = message["checkedInDate"] as? String {
            let formatter = ISO8601DateFormatter()
            formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
            self.checkedInDate = formatter.date(from: dateString)
        } else {
            self.checkedInDate = nil
        }
    }

    func requestState() {
        guard let session = session, session.isReachable else { return }
        session.sendMessage(["action": "getState"], replyHandler: { reply in
            DispatchQueue.main.async {
                self.updateState(from: reply)
            }
        }, errorHandler: nil)
    }

    // MARK: - WCSessionDelegate

    func session(_ session: WCSession, activationDidCompleteWith activationState: WCSessionActivationState, error: Error?) {
        DispatchQueue.main.async {
            self.isReachable = session.isReachable
            if session.isReachable {
                self.requestState()
            }
        }
    }

    func sessionReachabilityDidChange(_ session: WCSession) {
        DispatchQueue.main.async {
            self.isReachable = session.isReachable
            if session.isReachable {
                self.requestState()
            }
        }
    }

    func session(_ session: WCSession, didReceiveMessage message: [String: Any]) {
        DispatchQueue.main.async {
            self.updateState(from: message)
        }
    }

    func session(_ session: WCSession, didReceiveApplicationContext applicationContext: [String: Any]) {
        DispatchQueue.main.async {
            self.updateState(from: applicationContext)
        }
    }
}
