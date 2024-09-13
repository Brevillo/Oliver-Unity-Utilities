using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OliverBeebe.UnityUtilities.Editor
{
    [CreateAssetMenu(menuName = "Oliver Utilities/Editor/Work Timer")]
    public class WorkTimer : ScriptableObject
    {
        [SerializeField] private Segment activeSegment = new();
        [SerializeField] private List<Segment> savedSegments = new();

        [SerializeField] private DataName[] outputFormatting = new[]
        {
            DataName.Name,
            DataName.Description,
            DataName.Start,
            DataName.End,
            DataName.Duration,
        };

        private enum RecordingState
        {
            Inactive,
            Prepping,
            Recording,
        }

        [SerializeField] private RecordingState recordingState;
        [SerializeField] private bool outputFoldout;

        private enum DataName
        {
            Name,
            Description,
            Start,
            End,
            Duration,
        }

        private static readonly Dictionary<DataName, Func<Segment, string>> dataNameToValue = new()
        {
            { DataName.Name,        segment => segment.segmentName },
            { DataName.Description, segment => segment.description },
            { DataName.Start,       segment => segment.StartTime.ToString() },
            { DataName.End,         segment => segment.EndTime.ToString() },
            { DataName.Duration,    segment => segment.DurationSpan.ToString() }
        };

        [Serializable]
        private struct Segment
        {
            public void Restart()
            {
                end = start = Now.ToFileTime();
            }

            public void Update()
            {
                var endTime = Now;
                end = endTime.ToFileTime();
                duration = endTime.Subtract(DateTime.FromFileTime(start)).Ticks;
            }

            private static DateTime Now => DateTime.Now;

            public string segmentName;
            [TextArea(minLines: 3, maxLines: int.MaxValue)]
            public string description;
            public long start, end;
            public long duration;

            public bool copyData;
            public bool copied;
            public bool expanded;

            public readonly DateTime StartTime => DateTime.FromFileTime(start);
            public readonly DateTime EndTime => DateTime.FromFileTime(end);
            public readonly TimeSpan DurationSpan => TimeSpan.FromTicks(duration);

            public readonly TimeSpan ActiveDurationSpan => Now.Subtract(DateTime.FromFileTime(start));

            #region Editor
            #if UNITY_EDITOR

            [CustomPropertyDrawer(typeof(Segment))]
            private class SegmentPropertyDrawer : PropertyDrawer
            {
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    EditorGUI.BeginChangeCheck();

                    position.height = EditorGUIUtility.singleLineHeight;

                    var expandedProp = property.FindPropertyRelative(nameof(expanded));
                    expandedProp.boolValue = EditorGUI.Foldout(position, expandedProp.boolValue, "", true);

                    var nameProp = property.FindPropertyRelative(nameof(segmentName));
                    if (expandedProp.boolValue)
                    {
                        EditorGUI.PropertyField(position, nameProp);
                    }
                    else
                    {
                        EditorGUI.LabelField(position, nameProp.stringValue);
                    }

                    if (expandedProp.boolValue)
                    {
                        IncrementPosition(height: 6);
                        EditorGUI.PropertyField(position, property.FindPropertyRelative(nameof(description)));

                        IncrementPosition(spacing: 6);

                        var durationSpan = TimeSpan.FromTicks(property.FindPropertyRelative(nameof(duration)).longValue);
                        EditorGUI.LabelField(position, $"Total   {durationSpan}");

                        IncrementPosition();
                        TimeLabel(nameof(start), "Start   ");

                        IncrementPosition();
                        TimeLabel(nameof(end), "End     ");

                        IncrementPosition();
                        string copyButtonLabel = property.FindPropertyRelative(nameof(copied)).boolValue
                            ? "Data Copied to Clipboard! :)"
                            : "Copy Data to Clipboard";
                        property.FindPropertyRelative(nameof(copyData)).boolValue = GUI.Button(position, copyButtonLabel);
                    }

                    EditorGUI.EndChangeCheck();

                    void IncrementPosition(float spacing = 1, float height = 1)
                    {
                        position.height = EditorGUIUtility.singleLineHeight * height;
                        position.y += EditorGUIUtility.singleLineHeight * spacing + EditorGUIUtility.standardVerticalSpacing;
                    }

                    void TimeLabel(string name, string label)
                    {
                        var prop = property.FindPropertyRelative(name);
                        var time = DateTime.FromFileTime(prop.longValue);

                        EditorGUI.LabelField(position, $"{label}{time}");
                    }
                }

                public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
                {
                    (int lines, int spaces) = property.FindPropertyRelative(nameof(expanded)).boolValue
                        ? (11, 8)
                        : (1, 0);

                    return lines * EditorGUIUtility.singleLineHeight + spaces * EditorGUIUtility.standardVerticalSpacing;
                }
            }

            #endif
            #endregion
        }

        public void NewSegment()
        {
            activeSegment = new();
            recordingState = RecordingState.Prepping;
        }

        public void StartRecording()
        {
            if ((activeSegment.segmentName == default || activeSegment.segmentName == "")
                && (activeSegment.description == default || activeSegment.description == ""))
            {
                recordingState = RecordingState.Inactive;
            }
            else
            {
                activeSegment.Restart();
                recordingState = RecordingState.Recording;
            }
        }

        public void StopRecording()
        {
            recordingState = RecordingState.Inactive;


            savedSegments.Add(activeSegment);
        }

        #region Editor
        #if UNITY_EDITOR

        string GetSegmentData(Segment segment) => string.Join("\t", outputFormatting.Select(dataName => dataNameToValue[dataName].Invoke(segment)));

        [CustomEditor(typeof(WorkTimer))]
        private class WorkTimerEditor : UnityEditor.Editor
        {
            private WorkTimer Timer => target as WorkTimer;

            public override void OnInspectorGUI()
            {
                EditorGUI.BeginChangeCheck();
                serializedObject.UpdateIfRequiredOrScript();

                static bool Foldout(ref bool foldout, string label) => foldout = EditorGUILayout.Foldout(foldout, label, true);

                var state = Timer.recordingState;

                if (state == RecordingState.Inactive
                    && GUILayout.Button("New Segment"))
                {
                    Timer.NewSegment();
                }

                var activeSegmentProp = serializedObject.FindProperty(nameof(WorkTimer.activeSegment));
                var activeSegment = Timer.activeSegment;

                if (state != RecordingState.Inactive)
                {
                    EditorGUILayout.PropertyField(activeSegmentProp.FindPropertyRelative(nameof(Segment.segmentName)));
                    EditorGUILayout.PropertyField(activeSegmentProp.FindPropertyRelative(nameof(Segment.description)));

                    if (state == RecordingState.Recording)
                    {
                        EditorGUILayout.LabelField($"Total   {activeSegment.DurationSpan}");
                        EditorGUILayout.LabelField($"Start   {activeSegment.StartTime}");
                        EditorGUILayout.LabelField($"End     {activeSegment.EndTime}");
                    }

                    EditorGUILayout.Space();
                }


                if (state == RecordingState.Prepping
                    && GUILayout.Button("Start Recording"))
                {
                    Timer.StartRecording();
                }

                if (state == RecordingState.Recording)
                {
                    Timer.activeSegment.Update();

                    if (GUILayout.Button("Stop Recording"))
                    {
                        Timer.StopRecording();
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(savedSegments)));

                EditorGUILayout.Space();

                if (Foldout(ref Timer.outputFoldout, "Output"))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.LabelField(string.Join("  |  ", Timer.outputFormatting));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(outputFormatting)));

                    if (Timer.savedSegments != null && Timer.savedSegments.Count > 0)
                    {
                        string dataString = string.Join("\n", Timer.savedSegments.Select(Timer.GetSegmentData));

                        string copyButtonLabel = EditorGUIUtility.systemCopyBuffer == dataString
                            ? "Data Copied to Clipboard! :)"
                            : "Copy Data to Clipboard";

                        EditorGUILayout.LabelField("Output Preview");
                        GUI.enabled = false;
                        EditorGUILayout.TextArea(dataString, new GUIStyle(GUI.skin.textArea)
                        {
                            wordWrap = false,
                            fixedHeight = EditorGUIUtility.singleLineHeight * 5,
                            fontSize = 6,
                        });
                        GUI.enabled = true;

                        if (GUILayout.Button(copyButtonLabel))
                        {
                            EditorGUIUtility.systemCopyBuffer = dataString;
                        }

                        if (GUILayout.Button("Print Data to Console"))
                        {
                            Debug.Log(dataString);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No segments to output.", MessageType.Info);
                    }

                    EditorGUI.indentLevel--;
                }

                for (int i = 0; i < Timer.savedSegments.Count; i++)
                {
                    var segment = Timer.savedSegments[i];
                    string data = Timer.GetSegmentData(segment);

                    segment.copied = EditorGUIUtility.systemCopyBuffer == data;

                    if (segment.copyData)
                    {
                        EditorGUIUtility.systemCopyBuffer = Timer.GetSegmentData(segment);
                    }

                    Timer.savedSegments[i] = segment;
                }

                EditorGUI.EndChangeCheck();
                serializedObject.ApplyModifiedProperties();

                Repaint();
            }
        }

        #endif
        #endregion
    }
}
