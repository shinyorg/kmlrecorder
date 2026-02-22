import WidgetKit
import SwiftUI

// MARK: - Shared State

struct SharedState {
    static let suiteName = "group.org.shinylib.kmlrecorder"
    static let isRecordingKey = "isRecording"
    static let checkedInDateKey = "checkedInDate"

    static var isRecording: Bool {
        let defaults = UserDefaults(suiteName: suiteName)
        return defaults?.bool(forKey: isRecordingKey) ?? false
    }

    static var checkedInDate: Date? {
        guard let defaults = UserDefaults(suiteName: suiteName),
              let dateString = defaults.string(forKey: checkedInDateKey) else {
            return nil
        }
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return formatter.date(from: dateString)
    }
}

// MARK: - Timeline Entry

struct KmlRecorderEntry: TimelineEntry {
    let date: Date
    let isRecording: Bool
    let checkedInDate: Date?
}

// MARK: - Timeline Provider

struct KmlRecorderProvider: TimelineProvider {
    func placeholder(in context: Context) -> KmlRecorderEntry {
        KmlRecorderEntry(date: .now, isRecording: false, checkedInDate: nil)
    }

    func getSnapshot(in context: Context, completion: @escaping (KmlRecorderEntry) -> Void) {
        completion(KmlRecorderEntry(
            date: .now,
            isRecording: SharedState.isRecording,
            checkedInDate: SharedState.checkedInDate
        ))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<KmlRecorderEntry>) -> Void) {
        let entry = KmlRecorderEntry(
            date: .now,
            isRecording: SharedState.isRecording,
            checkedInDate: SharedState.checkedInDate
        )
        let nextUpdate = Calendar.current.date(byAdding: .minute, value: 15, to: .now)!
        completion(Timeline(entries: [entry], policy: .after(nextUpdate)))
    }
}

// MARK: - Widget Views

struct KmlRecorderWidgetView: View {
    var entry: KmlRecorderEntry

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 6) {
                Text("KML Recorder")
                    .font(.headline)
                    .foregroundColor(.primary)

                if entry.isRecording, let date = entry.checkedInDate {
                    Label("Recording", systemImage: "location.fill")
                        .font(.subheadline)
                        .foregroundColor(.green)

                    Text("Since \(date, style: .relative)")
                        .font(.caption)
                        .foregroundColor(.secondary)
                } else {
                    Label("Stopped", systemImage: "location.slash")
                        .font(.subheadline)
                        .foregroundColor(.secondary)
                }
            }

            Spacer()

            Link(destination: URL(string: "kmlrecorder://toggle")!) {
                Image(systemName: entry.isRecording ? "stop.circle.fill" : "record.circle")
                    .font(.system(size: 40))
                    .foregroundColor(entry.isRecording ? .red : .green)
            }
        }
        .padding()
    }
}

// MARK: - Widget

@main
struct KmlRecorderWidget: Widget {
    let kind: String = "KmlRecorderWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: KmlRecorderProvider()) { entry in
            if #available(iOS 17.0, *) {
                KmlRecorderWidgetView(entry: entry)
                    .containerBackground(.fill.tertiary, for: .widget)
            } else {
                KmlRecorderWidgetView(entry: entry)
                    .padding()
                    .background()
            }
        }
        .configurationDisplayName("KML Recorder")
        .description("Start and stop GPS tracking sessions.")
        .supportedFamilies([.systemMedium])
    }
}

// MARK: - Preview

#Preview(as: .systemMedium) {
    KmlRecorderWidget()
} timeline: {
    KmlRecorderEntry(date: .now, isRecording: false, checkedInDate: nil)
    KmlRecorderEntry(date: .now, isRecording: true, checkedInDate: .now.addingTimeInterval(-3600))
}
