using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OliverBeebe.UnityUtilities.Runtime.DebugWindow
{
    public class ConsoleModule : DefaultModule
    {
        public override string Name => "Console";

        private readonly List<ConsoleEntry> consoleEntries;

        public ConsoleModule(DebugWindowReferences references) : base(references)
        {
            consoleEntries = new();

            Application.logMessageReceived += OnLogMessageReceived;
        }

        protected override VisualElement CreateGUIInternal(VisualElement root)
        {
            root = base.CreateGUIInternal(root);

            consoleEntries.Clear();

            return root;
        }

        private readonly struct ConsoleEntry
        {
            public ConsoleEntry(string condition, string stackTrace, LogType type)
            {
                this.condition = condition;
                this.stackTrace = stackTrace;
                this.type = type;
            }

            public readonly string condition;
            public readonly string stackTrace;
            public readonly LogType type;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (content == null)
            {
                return;
            }

            consoleEntries.Add(new(condition, stackTrace, type));

            var consoleVisualElement = references.consoleElement.Instantiate();
            consoleVisualElement.Q<Label>("Message").text = condition;
            consoleVisualElement.Q<Label>("Stacktrace").text = stackTrace;

            content.Add(consoleVisualElement);
        }
    }
}
