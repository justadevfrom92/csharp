// "Tell listeners a download finished" - an event vs a plain public delegate.
public class NotifyListenersVariation
{
    // ORIGINAL style: "event". Outside code can only += (subscribe) and -= (unsubscribe).
    public event Action<string>? FinishedEvent;

    // A plain public delegate FIELD: outside code can ALSO call it (pretend a download finished)
    // and REPLACE it with "=" (silently removing everyone else's handler). "event" exists to prevent both.
    public Action<string>? FinishedDelegate;

    public void Finish(string fileName)
    {
        FinishedEvent?.Invoke(fileName);
        FinishedDelegate?.Invoke(fileName);
    }
}
