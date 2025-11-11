using Markdown.Entities;
using System.Text;
using static Markdown.Inlines.InlineSyntax;

namespace Markdown.Inlines;

public class InlineMarkerProcessor(string input, List<Node> nodes, StringBuilder buffer)
{
    private readonly string _input = input;
    private readonly List<Node> _nodes = nodes;
    private readonly StringBuilder _buffer = buffer;

    private bool _isStrongOpen;
    private bool _isEmOpen;

    private int _strongStartIndex;
    private int _emStartIndex;

    private bool _strongOpenedInsideWord;
    private bool _emOpenedInsideWord;

    private bool _strongSawWhitespace;
    private bool _emSawWhitespace;

    private bool _emSawStrongCloser;

    public void OnWhitespace()
    {
        if (_isStrongOpen) _strongSawWhitespace = true;
        if (_isEmOpen) _emSawWhitespace = true;
    }

    public bool TryHandleStrong(ref int i)
    {
        if (_isEmOpen)
        {
            _buffer.Append(StrongMarker);
            _emSawStrongCloser = true;
            i += 2;
            return true;
        }

        if (ShouldCloseStrong(i))
        {
            CloseStrong(ref i);
            return true;
        }

        if (ShouldOpenStrong(i))
        {
            OpenStrong(ref i);
            return true;
        }

        _buffer.Append(StrongMarker);
        i += 2;
        return true;
    }

    public bool TryHandleEm(ref int i)
    {
        if (ShouldCloseEm(i))
        {
            CloseEm(ref i);
            return true;
        }

        if (ShouldOpenEm(i))
        {
            OpenEm(ref i);
            return true;
        }

        _buffer.Append(Underscore);
        i += 1;
        return true;
    }

    public void FinalizeUnclosedMarkers()
    {
        if (!_isStrongOpen && !_isEmOpen)
            return;

        var inserts = new List<(int index, string text)>();
        if (_isStrongOpen) inserts.Add((_strongStartIndex, StrongMarker));
        if (_isEmOpen) inserts.Add((_emStartIndex, EmMarker));

        _buffer.InsertFromEnd(inserts);

        ResetMarkerState();
    }

    private bool ShouldCloseStrong(int i)
    {
        return _isStrongOpen &&
            CanOpenOrCloseMarker(_input, i, 2, open: false) &&
            !IsCrossingWords(_strongOpenedInsideWord, IsWordChar(GetNextChar(_input, i, 2)), _strongSawWhitespace) &&
            _buffer.Length > _strongStartIndex;
    }

    private bool ShouldOpenStrong(int i)
    {
        return !_isStrongOpen && CanOpenOrCloseMarker(_input, i, 2, open: true);
    }

    private void CloseStrong(ref int i)
    {
        var content = _buffer.ToString(_strongStartIndex, _buffer.Length - _strongStartIndex);
        _buffer.Length = _strongStartIndex;

        _buffer.CommitText(_nodes);
        _nodes.Add(new Node(content, NodeType.Strong));

        _isStrongOpen = false;
        _strongSawWhitespace = false;
        i += 2;
    }

    private void OpenStrong(ref int i)
    {
        _isStrongOpen = true;
        _strongStartIndex = _buffer.Length;
        _strongOpenedInsideWord = IsWordChar(GetPrevChar(_input, i));
        _strongSawWhitespace = false;
        i += 2;
    }

    private bool ShouldCloseEm(int i)
    {
        return _isEmOpen &&
            CanOpenOrCloseMarker(_input, i, 1, open: false) &&
            !IsCrossingWords(_emOpenedInsideWord, IsWordChar(GetNextChar(_input, i, 1)), _emSawWhitespace) &&
            _buffer.Length > _emStartIndex;
    }

    private bool ShouldOpenEm(int i)
    {
        return !_isEmOpen && CanOpenOrCloseMarker(_input, i, 1, open: true);
    }

    private void CloseEm(ref int i)
    {
        // Случай конфликта Strong внутри Em
        if (_isStrongOpen && _strongStartIndex < _emStartIndex && _emSawStrongCloser)
        {
            var inserts = new List<(int index, string text)>
            {
                (_strongStartIndex, StrongMarker),
                (_emStartIndex, EmMarker)
            };

            _buffer.InsertFromEnd(inserts);
            ResetMarkerState();
            _buffer.Append(Underscore);
            i += 1;
            return;
        }

        // Нормальное закрытие Em
        var emContent = _buffer.ToString(_emStartIndex, _buffer.Length - _emStartIndex);

        if (_isStrongOpen && _strongStartIndex < _emStartIndex)
        {
            // Strong до Em — сначала закрываем Strong
            var strongBeforeEm = _buffer.ToString(_strongStartIndex, _emStartIndex - _strongStartIndex);
            _buffer.Length = _strongStartIndex;
            _buffer.CommitText(_nodes);
            _nodes.Add(new Node(strongBeforeEm, NodeType.Strong));
            _strongStartIndex = _buffer.Length;
        }
        else
        {
            _buffer.Length = _emStartIndex;
            _buffer.CommitText(_nodes);
        }

        _nodes.Add(new Node(emContent, NodeType.Em));

        _isEmOpen = false;
        _emSawWhitespace = false;
        _emSawStrongCloser = false;
        i += 1;
    }

    private void OpenEm(ref int i)
    {
        _isEmOpen = true;
        _emStartIndex = _buffer.Length;
        _emOpenedInsideWord = IsWordChar(GetPrevChar(_input, i));
        _emSawWhitespace = false;
        _emSawStrongCloser = false;
        i += 1;
    }

    private void ResetMarkerState()
    {
        _isStrongOpen = false;
        _isEmOpen = false;
        _strongSawWhitespace = false;
        _emSawWhitespace = false;
        _emSawStrongCloser = false;
    }
}
