using System;
using System.Reflection;
using GateVR.Application;
using GateVR.Domain.Entities;
using GateVR.Domain.Interfaces;

internal static class Phase1LogicTests
{
    private static int _failures;

    public static int Main()
    {
        Console.WriteLine("Fase 1 — motor lógico sin UnityEngine");
        TestAndPropagation();
        TestOrPropagation();
        TestNotAndCascade();
        TestGateUsesEvaluate();
        TestConnectionRules();
        TestCycleRejectionKeepsPreviousCable();
        TestExerciseValidation();
        TestDirectSwitchIsRejectedWithoutMutating();
        TestResetCircuit();

        if (_failures == 0)
        {
            Console.WriteLine("RESULTADO: todas las pruebas pasaron.");
            return 0;
        }

        Console.WriteLine("RESULTADO: " + _failures + " prueba(s) fallaron.");
        return 1;
    }

    private static void TestAndPropagation()
    {
        Section("AND Switch -> Gate -> LED");
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        GateNode gate = new GateNode(new AndGate());
        LedNode led = new LedNode();
        bool lastLit = true;
        int changes = 0;
        led.OnLitStateChanged += lit =>
        {
            lastLit = lit;
            changes++;
        };

        Check(gate.RequiredInputCount == 2, "AND pide 2 entradas");
        Check(gate.TryConnectInputA(a).IsValid, "conecta A");
        Check(led.TryConnect(gate).IsValid, "LED escucha la compuerta antes de cerrar el circuito");
        Check(!led.IsLit, "con una sola entrada el LED sigue apagado");

        Check(gate.TryConnectInputB(b).IsValid, "conecta B");
        Check(!gate.GetSignal() && !led.IsLit, "00 -> 0");

        a.SetState(true);
        Check(!gate.GetSignal() && !led.IsLit && !lastLit, "10 -> 0");
        b.SetState(true);
        Check(gate.GetSignal() && led.IsLit && lastLit, "11 -> 1");
        a.Toggle();
        Check(!a.IsOn && !led.IsLit, "01 -> 0");
        Check(changes >= 2, "el LED notificó el encendido y el apagado");
    }

    private static void TestOrPropagation()
    {
        Section("OR Switch -> Gate -> LED");
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        GateNode gate = new GateNode(new OrGate());
        LedNode led = new LedNode();
        Check(ConnectBinary(gate, a, b, led), "cableado OR");

        Check(!led.IsLit, "00 -> 0");
        b.SetState(true);
        Check(led.IsLit && gate.GetSignal(), "01 -> 1");
        b.SetState(false);
        a.SetState(true);
        Check(led.IsLit, "10 -> 1");
        b.SetState(true);
        Check(led.IsLit, "11 -> 1");
    }

    private static void TestNotAndCascade()
    {
        Section("Cascada Switch -> NOT -> AND -> LED");
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        GateNode not = new GateNode(new NotGate());
        GateNode and = new GateNode(new AndGate());
        LedNode led = new LedNode();

        var rejected = not.TryConnectInputB(a);
        Check(!rejected.IsValid && not.InputB == null, "NOT rechaza la entrada B: " + rejected.ErrorMessage);
        Check(not.RequiredInputCount == 1, "NOT pide 1 entrada");
        Check(not.TryConnectInputA(a).IsValid, "NOT entrada A");
        Check(and.TryConnectInputA(not).IsValid, "AND recibe NOT");
        Check(and.TryConnectInputB(b).IsValid, "AND recibe B");
        Check(led.TryConnect(and).IsValid, "LED recibe AND");

        Check(not.GetSignal() && !led.IsLit, "A=0 invierte a 1 y AND queda en 0");
        b.SetState(true);
        Check(led.IsLit && and.GetSignal(), "A=0 B=1 -> NOT=1 AND=1");
        a.SetState(true);
        Check(!not.GetSignal() && !led.IsLit, "A=1 B=1 -> NOT=0 AND=0");

        GateNode namedNot = new GateNode(new NameOnlyNotGate());
        Check(namedNot.RequiredInputCount == 1, "una compuerta llamada NOT también pide 1 entrada");
    }

    private static void TestGateUsesEvaluate()
    {
        Section("GateNode delega en ILogicGate.Evaluate");
        ProbeGate probe = new ProbeGate();
        GateNode gate = new GateNode(probe);
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        a.SetState(true);
        b.SetState(true);

        gate.TryConnectInputA(a);
        Check(probe.Calls == 0, "con una entrada faltante no evalúa la compuerta");
        gate.TryConnectInputB(b);
        Check(probe.Calls == 1, "al completar las entradas llama a Evaluate");
        Check(gate.GetSignal(), "la salida queda en 1");
        int callsBeforeToggle = probe.Calls;
        a.SetState(false);
        Check(probe.Calls == callsBeforeToggle + 1, "un cambio de entrada vuelve a evaluar");
        Check(!gate.GetSignal(), "la salida pasa a 0");
    }

    private static void TestConnectionRules()
    {
        Section("Reglas de conexión y ciclos");
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        GateNode gate = new GateNode(new AndGate());

        var first = gate.TryConnectInputA(a);
        var occupied = gate.TryConnectInputA(b);
        Check(first.IsValid, "primer cable aceptado");
        Check(!occupied.IsValid && ReferenceEquals(gate.InputA, a), "el segundo cable se rechaza: " + occupied.ErrorMessage);
        Check(SubscriberCount(a) == 1, "la fuente no queda suscrita dos veces");

        var replaced = gate.ReplaceInputA(b);
        Check(replaced.IsValid && ReferenceEquals(gate.InputA, b), "ReplaceInputA cambia el cable");
        Check(SubscriberCount(a) == 0 && SubscriberCount(b) == 1, "la fuente anterior queda desuscrita");

        var self = gate.TryConnectInputB(gate);
        Check(!self.IsValid && gate.InputB == null, "rechaza el lazo directo: " + self.ErrorMessage);

        GateNode downstream = new GateNode(new OrGate());
        Check(downstream.TryConnectInputA(gate).IsValid, "B puede escuchar a A");
        var back = gate.TryConnectInputB(downstream);
        Check(!back.IsValid && gate.InputB == null, "rechaza A <- B cuando B ya depende de A: " + back.ErrorMessage);

        GateNode third = new GateNode(new AndGate());
        Check(third.TryConnectInputA(downstream).IsValid, "C escucha a B");
        var longCycle = gate.ReplaceInputA(third);
        Check(!longCycle.IsValid && ReferenceEquals(gate.InputA, b), "rechaza el ciclo A-B-C y conserva el cable previo");

        GateNode sibling = new GateNode(new AndGate());
        Check(sibling.TryConnectInputA(a).IsValid, "otra compuerta puede compartir el interruptor");
        Check(gate.TryConnectInputB(sibling).IsValid, "no es ciclo si la fuente no depende de este nodo");

        LedNode led = new LedNode();
        Check(led.TryConnect(null).IsValid == false, "LED rechaza una fuente nula");
        Check(led.TryConnect(gate).IsValid, "LED acepta la compuerta");
        Check(!led.TryConnect(a).IsValid && ReferenceEquals(led.Input, gate), "LED rechaza un segundo cable");

        a.SetState(true);
        sibling.TryConnectInputB(b);
        b.SetState(true);
        Check(led.IsLit, "el reemplazo y el fan-out siguen propagando hasta el LED");
        led.Disconnect();
        bool litAfterDisconnect = led.IsLit;
        a.SetState(false);
        Check(!litAfterDisconnect && !led.IsLit && led.Input == null, "al desconectar, el LED deja de seguir la compuerta");
        Check(!gate.GetSignal(), "la compuerta sí vio el apagado del interruptor");
    }

    private static void TestCycleRejectionKeepsPreviousCable()
    {
        Section("Un ciclo rechazado no corta el cable que ya funcionaba");
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        a.SetState(true);
        b.SetState(true);
        GateNode and = new GateNode(new AndGate());
        LedNode led = new LedNode();
        and.TryConnectInputA(a);
        and.TryConnectInputB(b);
        led.TryConnect(and);
        Check(led.IsLit, "AND encendido antes del intento de ciclo");

        GateNode not = new GateNode(new NotGate());
        not.TryConnectInputA(and);
        var rejected = and.ReplaceInputA(not);
        Check(!rejected.IsValid && ReferenceEquals(and.InputA, a), rejected.ErrorMessage);
        a.SetState(false);
        Check(!led.IsLit, "el interruptor original sigue gobernando el LED");
    }

    private static void TestExerciseValidation()
    {
        Section("Validación estructural y tabla de verdad");
        PracticeSessionService session = new PracticeSessionService();
        Check(session.Exercises.Count == 2, "hay dos ejercicios");
        Check(session.CurrentExercise.Id == "and-01", "el ejercicio inicial es AND");

        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        GateNode and = new GateNode(new AndGate());
        LedNode led = new LedNode();
        and.TryConnectInputA(a);
        and.TryConnectInputB(b);
        led.TryConnect(and);
        a.SetState(true);

        var success = session.ValidateCurrentCircuit(a, b, and, led);
        Check(success.IsSuccess, success.FeedbackMessage);
        Check(a.IsOn && !b.IsOn && !led.IsLit, "restaura A=1, B=0 y el LED de esa combinación");

        session.SelectExercise("or-01");
        var wrongExercise = session.ValidateCurrentCircuit(a, b, and, led);
        Check(!wrongExercise.IsSuccess && wrongExercise.FeedbackMessage.Contains("requiere una compuerta OR"), wrongExercise.FeedbackMessage);
        Check(a.IsOn && !b.IsOn, "el rechazo estructural no altera los interruptores");

        session.SelectExercise("AND-01");
        GateNode or = new GateNode(new OrGate());
        LedNode orLed = new LedNode();
        or.TryConnectInputA(a);
        or.TryConnectInputB(b);
        orLed.TryConnect(or);
        var wrongType = session.ValidateCurrentCircuit(a, b, or, orLed);
        Check(!wrongType.IsSuccess && wrongType.FeedbackMessage.Contains("requiere una compuerta AND"), wrongType.FeedbackMessage);

        GateNode partial = new GateNode(new AndGate());
        LedNode partialLed = new LedNode();
        partial.TryConnectInputA(a);
        partialLed.TryConnect(partial);
        var missing = session.ValidateCurrentCircuit(a, b, partial, partialLed);
        Check(!missing.IsSuccess && missing.FeedbackMessage.Contains("1 de 2"), missing.FeedbackMessage);

        GateNode sameInput = new GateNode(new AndGate());
        LedNode sameLed = new LedNode();
        sameInput.TryConnectInputA(a);
        sameInput.TryConnectInputB(a);
        sameLed.TryConnect(sameInput);
        var missingB = session.ValidateCurrentCircuit(a, b, sameInput, sameLed);
        Check(!missingB.IsSuccess && missingB.FeedbackMessage.Contains("interruptor B"), missingB.FeedbackMessage);

        SwitchNode c = new SwitchNode();
        GateNode inverted = new GateNode(new AndGate());
        GateNode inverter = new GateNode(new NotGate());
        LedNode invertedLed = new LedNode();
        inverter.TryConnectInputA(a);
        inverted.TryConnectInputA(inverter);
        inverted.TryConnectInputB(c);
        invertedLed.TryConnect(inverted);
        a.SetState(true);
        c.SetState(true);
        var cascadeMismatch = session.ValidateCurrentCircuit(a, c, inverted, invertedLed);
        Check(a.IsOn && c.IsOn, "una tabla fallida restaura los interruptores");
        Check(!cascadeMismatch.IsSuccess, cascadeMismatch.FeedbackMessage);
        Check(cascadeMismatch.FeedbackMessage.Contains("A=0, B=1")
            && cascadeMismatch.FeedbackMessage.Contains("Se esperaba 0")
            && cascadeMismatch.FeedbackMessage.Contains("mostró 1"),
            "la tabla observa la cascada NOT: " + cascadeMismatch.FeedbackMessage);

        session.SelectExercise("or-01");
        GateNode orGate = new GateNode(new OrGate());
        LedNode orOk = new LedNode();
        orGate.TryConnectInputA(a);
        orGate.TryConnectInputB(b);
        orOk.TryConnect(orGate);
        var orSuccess = session.ValidateCurrentCircuit(a, b, orGate, orOk);
        Check(orSuccess.IsSuccess, orSuccess.FeedbackMessage);
    }

    private static void TestDirectSwitchIsRejectedWithoutMutating()
    {
        Section("Switch directo al LED");
        PracticeSessionService session = new PracticeSessionService();
        session.SelectExercise("and-01");
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        a.SetState(true);
        b.SetState(true);
        GateNode and = new GateNode(new AndGate());
        LedNode led = new LedNode();
        and.TryConnectInputA(a);
        and.TryConnectInputB(b);
        led.TryConnect(a);

        var result = session.ValidateCurrentCircuit(a, b, and, led);
        Check(!result.IsSuccess && result.FeedbackMessage.Contains("directamente"), result.FeedbackMessage);
        Check(a.IsOn && b.IsOn, "el rechazo estructural no mueve los interruptores");
        Check(led.IsLit, "el LED sigue mostrando el interruptor, no una combinación de prueba");
    }

    private static void TestResetCircuit()
    {
        Section("ResetCircuit");
        PracticeSessionService session = new PracticeSessionService();
        SwitchNode a = new SwitchNode();
        SwitchNode b = new SwitchNode();
        GateNode and = new GateNode(new AndGate());
        LedNode led = new LedNode();
        a.SetState(true);
        b.SetState(true);
        and.TryConnectInputA(a);
        and.TryConnectInputB(b);
        led.TryConnect(and);
        Check(led.IsLit, "el circuito está encendido antes del reset");

        session.ResetCircuit(a, b, and, led);
        Check(!a.IsOn && !b.IsOn, "los interruptores quedan apagados");
        Check(and.InputA == null && and.InputB == null && led.Input == null, "no quedan cables");
        Check(!led.IsLit && !and.GetSignal(), "la salida queda apagada");

        a.SetState(true);
        b.SetState(true);
        Check(!led.IsLit, "sin cables, los interruptores ya no encienden el LED");

        var afterReset = session.ValidateCurrentCircuit(a, b, and, led);
        Check(!afterReset.IsSuccess && afterReset.FeedbackMessage.Contains("no tiene una fuente"), afterReset.FeedbackMessage);
    }

    private static bool ConnectBinary(GateNode gate, SwitchNode a, SwitchNode b, LedNode led)
    {
        return gate.TryConnectInputA(a).IsValid
            && gate.TryConnectInputB(b).IsValid
            && led.TryConnect(gate).IsValid;
    }

    private static int SubscriberCount(object source)
    {
        FieldInfo field = source.GetType().GetField(
            "OnSignalChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            throw new InvalidOperationException("No se encontró el campo del evento OnSignalChanged.");

        Delegate handler = field.GetValue(source) as Delegate;
        if (handler == null)
            return 0;

        return handler.GetInvocationList().Length;
    }

    private static void Section(string title)
    {
        Console.WriteLine("");
        Console.WriteLine(title);
    }

    private static void Check(bool condition, string message)
    {
        if (condition)
        {
            Console.WriteLine("  OK   " + message);
            return;
        }

        _failures++;
        Console.WriteLine("  FAIL " + message);
    }

    private sealed class ProbeGate : ILogicGate
    {
        public int Calls;

        public string Name
        {
            get { return "PROBE"; }
        }

        public bool Evaluate(bool[] inputs)
        {
            Calls++;
            return inputs != null && inputs.Length == 2 && inputs[0] && inputs[1];
        }
    }

    private sealed class NameOnlyNotGate : ILogicGate
    {
        public string Name
        {
            get { return "NOT"; }
        }

        public bool Evaluate(bool[] inputs)
        {
            return inputs != null && inputs.Length > 0 && !inputs[0];
        }
    }
}
