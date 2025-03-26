#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace Kamgam.PolygonMaterialPainter
{
    public interface IUndoStack
    {
        public void Clear();
        public bool HasUndoActions();
        public bool IsEmpty();
        public bool HasRedoActions();
        public void Undo();
        public void Redo();
    }

    public class UndoStack<TInput,TState> : IUndoStack
    {
        /// <summary>
        /// List of states
        /// </summary>
        protected LinkedList<TState> _undoStack = new LinkedList<TState>();
        protected LinkedList<TState> _redoStack = new LinkedList<TState>();

        protected double _lastSelectionUndoRegistrationTime = 0;

        protected System.Func<TInput,TState> _createStateFunc;
        protected System.Action<TState> _revertToStateFunc;
        public int MaxEntries;

        /// <summary>
        /// A custom undo redo stack.
        /// </summary>
        /// <param name="createStateFunc">A function that returns a copy of the current state. This copy will be stored in the undo stack.</param>
        /// <param name="revertToStateFunc">A function to apply the given value as the new current state.
        /// The parameter will be one of the copies created by the 'copyStateFunc'.
        /// You may want to copy the given value again if it is a reference type.</param>
        public UndoStack(System.Func<TInput,TState> createStateFunc, System.Action<TState> revertToStateFunc, int maxEntries = 20)
        {
            _createStateFunc = createStateFunc;
            _revertToStateFunc = revertToStateFunc;
            MaxEntries = maxEntries;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public TState Peek()
        {
            if (HasUndoActions())
            {
                return _undoStack.Last.Value;
            }    
            else
            {
                return default;
            }
        }

        public bool HasUndoActions()
        {
            return _undoStack.Count > 0;
        }

        public bool IsEmpty()
        {
            return !HasUndoActions() && !HasRedoActions();
        }

        public bool HasRedoActions()
        {
            return _redoStack.Count > 0;
        }

        public void Add(TState data)
        {
            if (_undoStack.Count > MaxEntries)
            {
                _undoStack.RemoveFirst();
            }

            _undoStack.AddLast(data);
            _lastSelectionUndoRegistrationTime = EditorApplication.timeSinceStartup;
            _redoStack.Clear();
        }

        /// <summary>
        /// Saves the current state as an undo point to return to.<br />
        /// Call this AFTER the action you wish to undo.<br />
        /// Actually if the undo stack is empty then you should
        /// call this BEFORE AND AFTER the action so you will
        /// ensure that there are at least two states (for undo and redo).
        /// </summary>
        /// <param name="input">The input which will be forwarded to the createStateFunc</param>
        /// <param name="minTimeDelta">Register a new undo if the last one is older than # seconds, otherwise update the current one.</param>
        /// <param name="forceNewGroup"></param>
        public void Record(TInput input, double minTimeDelta = 0d, bool forceNewGroup = false)
        {
            if (_undoStack.Count > MaxEntries)
            {
                _undoStack.RemoveFirst();
            }

            if (forceNewGroup)
            {
                var copy = _createStateFunc.Invoke(input);
                _undoStack.AddLast(copy);
            }
            else
            {
                // Register a new undo if the last one is older than # seconds, otherwise update the current one.
                if (EditorApplication.timeSinceStartup - _lastSelectionUndoRegistrationTime < minTimeDelta)
                {
                    if (_undoStack.Count > 0)
                    {
                        _undoStack.RemoveLast();
                    }
                }
                var copy = _createStateFunc.Invoke(input);
                _undoStack.AddLast(copy);
            }
            _lastSelectionUndoRegistrationTime = EditorApplication.timeSinceStartup;
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count > 0) 
            {
                if (_undoStack.Count > 1)
                {
                    // We move the last undo state to redo.
                    var last = _undoStack.Last.Value;
                    _undoStack.RemoveLast();
                    _redoStack.AddLast(last);
                }

                // And then call undo on the previous state.
                // This assumes that UNDOs are recorded AFTER the action, not before.
                if (_undoStack.Count > 0)
                {
                    _revertToStateFunc.Invoke(_undoStack.Last.Value);
                }
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var last = _redoStack.Last.Value;
                _redoStack.RemoveLast();
                _undoStack.AddLast(last);
                _revertToStateFunc(last);
            }
        }
    }
}
#endif
