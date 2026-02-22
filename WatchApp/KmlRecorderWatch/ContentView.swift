import SwiftUI

struct ContentView: View {
    @EnvironmentObject var connectivity: WatchConnectivityManager

    var body: some View {
        VStack(spacing: 16) {
            // Status icon
            Image(systemName: connectivity.isRecording ? "location.fill" : "location.slash")
                .font(.system(size: 36))
                .foregroundColor(connectivity.isRecording ? .green : .gray)

            // Status text
            Text(connectivity.isRecording ? "Recording" : "Stopped")
                .font(.headline)

            // Elapsed time
            if connectivity.isRecording, let date = connectivity.checkedInDate {
                Text("Since \(date, style: .relative)")
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }

            // Toggle button
            Button(action: {
                connectivity.toggleTracking()
            }) {
                HStack {
                    Image(systemName: connectivity.isRecording ? "stop.fill" : "record.circle")
                    Text(connectivity.isRecording ? "Check Out" : "Check In")
                }
            }
            .buttonStyle(.borderedProminent)
            .tint(connectivity.isRecording ? .red : .green)
            .disabled(!connectivity.isReachable)

            if !connectivity.isReachable {
                Text("iPhone not reachable")
                    .font(.caption2)
                    .foregroundColor(.orange)
            }
        }
        .padding()
    }
}

#Preview {
    ContentView()
        .environmentObject(WatchConnectivityManager())
}
