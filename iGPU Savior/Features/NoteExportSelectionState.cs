using System.Collections.Generic;
using System.Linq;

namespace PotatoOptimization.Features
{
    public class NoteExportSelectionState
    {
        private readonly HashSet<ulong> _selectedPageIds = new HashSet<ulong>();

        public bool IsSelectionMode { get; private set; }

        public IReadOnlyCollection<ulong> SelectedPageIds => _selectedPageIds.ToArray();

        public void EnterSelectionMode()
        {
            IsSelectionMode = true;
            _selectedPageIds.Clear();
        }

        public void ToggleSelection(ulong pageId)
        {
            if (!IsSelectionMode)
            {
                return;
            }

            if (!_selectedPageIds.Add(pageId))
            {
                _selectedPageIds.Remove(pageId);
            }
        }

        public void ExitSelectionMode()
        {
            IsSelectionMode = false;
            _selectedPageIds.Clear();
        }

        public bool IsSelected(ulong pageId)
        {
            return _selectedPageIds.Contains(pageId);
        }
    }
}
