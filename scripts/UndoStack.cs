using System;
using System.Collections.Generic;

namespace Slide;

public interface IEditorCommand
{
    void Do();
    void Undo();
}

// Convenience wrapper for one-off commands that don't need their own class.
public class SimpleCommand : IEditorCommand
{
    private readonly Action _do;
    private readonly Action _undo;

    public SimpleCommand(Action @do, Action undo) { _do = @do; _undo = undo; }

    public void Do()   => _do();
    public void Undo() => _undo();
}

public class UndoStack
{
    private readonly Stack<IEditorCommand> _undo = new();
    private readonly Stack<IEditorCommand> _redo = new();
    private readonly Action                _onChanged;
    private int                            _savedAt = 0;

    public bool IsModified => _undo.Count != _savedAt;
    public bool CanUndo    => _undo.Count > 0;
    public bool CanRedo    => _redo.Count > 0;

    public UndoStack(Action onChanged) => _onChanged = onChanged;

    public void Execute(IEditorCommand cmd)
    {
        // If the save point is in redo history that's about to be cleared, it's unreachable.
        if (_savedAt > _undo.Count)
            _savedAt = -1;

        cmd.Do();
        _undo.Push(cmd);
        _redo.Clear();
        _onChanged();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        var cmd = _undo.Pop();
        cmd.Undo();
        _redo.Push(cmd);
        _onChanged();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        var cmd = _redo.Pop();
        cmd.Do();
        _undo.Push(cmd);
        _onChanged();
    }

    // Use when the change has already been applied visually — only track it for undo/redo.
    public void ExecuteAlreadyDone(IEditorCommand cmd)
    {
        if (_savedAt > _undo.Count) _savedAt = -1;
        _undo.Push(cmd);
        _redo.Clear();
        _onChanged();
    }

    // Call after a successful save so undo/redo can track whether we're back at clean state.
    public void MarkSavePoint() => _savedAt = _undo.Count;

    // Call when loading a new level — wipes all history.
    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _savedAt = 0;
        _onChanged();
    }
}
