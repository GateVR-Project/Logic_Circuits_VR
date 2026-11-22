using System;
using System.Collections.Generic;

namespace GateVR.Application
{
    /// <summary>
    /// Calcula el estado de las tareas a partir de hechos de la sesión.
    /// Una tarea completada vuelve a pendiente cuando falla su condición o una dependencia.
    /// </summary>
    public sealed class LessonProgress
    {
        private PracticeExercise _exercise;
        private bool _acknowledged;
        private bool _studentConfirmedGate;
        private bool _wired;
        private bool _triedSwitches;
        private bool _validationFresh;
        private List<TaskStatus> _tasks = new List<TaskStatus>();

        public LessonProgress(PracticeExercise exercise)
        {
            Load(exercise);
        }

        public int CompletedCount { get; private set; }

        public int TotalCount { get; private set; }

        public bool IsPracticeComplete
        {
            get { return TotalCount > 0 && CompletedCount == TotalCount; }
        }

        public IReadOnlyList<TaskStatus> Tasks
        {
            get { return _tasks.AsReadOnly(); }
        }

        public void Load(PracticeExercise exercise)
        {
            if (exercise == null)
                throw new ArgumentNullException(nameof(exercise));

            _exercise = exercise;
            _acknowledged = false;
            _studentConfirmedGate = false;
            _wired = false;
            _triedSwitches = false;
            _validationFresh = false;
            Rebuild();
        }

        public void AcknowledgeRequirement()
        {
            _acknowledged = true;
            Rebuild();
        }

        /// <summary>
        /// Solo la pulsación manual del estudiante. El cambio automático de Siguiente no llama aquí.
        /// </summary>
        public void NoteManualGate(string gateName)
        {
            bool matches = !string.IsNullOrEmpty(gateName)
                && string.Equals(gateName, _exercise.RequiredGateType, StringComparison.OrdinalIgnoreCase);

            if (_studentConfirmedGate != matches)
                _validationFresh = false;

            _studentConfirmedGate = matches;
            Rebuild();
        }

        public void NoteWiring(bool wired)
        {
            if (_wired != wired)
                _validationFresh = false;

            _wired = wired;
            Rebuild();
        }

        /// <summary>
        /// Un interruptor accionado por el estudiante. La tabla de verdad no llama aquí.
        /// </summary>
        public void NoteManualSwitch()
        {
            _triedSwitches = true;
            _validationFresh = false;
            Rebuild();
        }

        public void NoteValidation(bool success)
        {
            _validationFresh = success;
            Rebuild();
        }

        /// <summary>
        /// Reiniciar quita cables y apaga interruptores. La lectura y la compuerta confirmada se conservan.
        /// </summary>
        public void NoteCircuitReset()
        {
            _wired = false;
            _triedSwitches = false;
            _validationFresh = false;
            Rebuild();
        }

        private void Rebuild()
        {
            IReadOnlyList<PracticeTaskDefinition> definitions = _exercise.Tasks;
            var completed = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Count; i++)
                completed[definitions[i].Id] = false;

            for (int pass = 0; pass < definitions.Count; pass++)
            {
                bool changed = false;
                for (int i = 0; i < definitions.Count; i++)
                {
                    PracticeTaskDefinition task = definitions[i];
                    if (completed[task.Id])
                        continue;

                    if (!ConditionHolds(task.Condition) || !DependenciesMet(task, completed))
                        continue;

                    completed[task.Id] = true;
                    changed = true;
                }

                if (!changed)
                    break;
            }

            var ordered = new List<PracticeTaskDefinition>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
                ordered.Add(definitions[i]);

            ordered.Sort(delegate(PracticeTaskDefinition left, PracticeTaskDefinition right)
            {
                return left.Order.CompareTo(right.Order);
            });

            var statuses = new List<TaskStatus>(ordered.Count);
            int done = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                bool isDone = completed[ordered[i].Id];
                if (isDone)
                    done++;

                statuses.Add(new TaskStatus(
                    ordered[i],
                    isDone ? LessonTaskState.Completed : LessonTaskState.Pending));
            }

            _tasks = statuses;
            CompletedCount = done;
            TotalCount = statuses.Count;
        }

        private bool ConditionHolds(TaskConditionKind condition)
        {
            switch (condition)
            {
                case TaskConditionKind.ReadRequirement:
                    return _acknowledged;
                case TaskConditionKind.ConfirmGate:
                    return _studentConfirmedGate;
                case TaskConditionKind.WireCircuit:
                    return _wired;
                case TaskConditionKind.TrySwitches:
                    return _triedSwitches;
                case TaskConditionKind.CheckResult:
                    return _validationFresh;
                default:
                    return false;
            }
        }

        private static bool DependenciesMet(PracticeTaskDefinition task, Dictionary<string, bool> completed)
        {
            IReadOnlyList<string> dependencies = task.DependsOn;
            for (int i = 0; i < dependencies.Count; i++)
            {
                bool done;
                if (!completed.TryGetValue(dependencies[i], out done) || !done)
                    return false;
            }

            return true;
        }
    }
}
