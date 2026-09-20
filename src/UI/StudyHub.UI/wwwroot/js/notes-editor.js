// Backs the Notes editor's "/" command menu. A plain <textarea> exposes no
// cursor pixel position, so the caret-coordinate lookup uses the standard
// "mirror div" technique: an offscreen clone of the textarea's text metrics
// with a marker span inserted at the caret, whose position is then read back.
window.studyHubNotesEditor = {
  getSelectionStart: function (el) {
    return el.selectionStart;
  },

  getCaretCoordinates: function (el) {
    const style = getComputedStyle(el);
    const mirror = document.createElement('div');
    const mirroredProps = [
      'boxSizing', 'width', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
      'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth',
      'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 'letterSpacing',
    ];
    mirroredProps.forEach(function (prop) { mirror.style[prop] = style[prop]; });
    mirror.style.position = 'absolute';
    mirror.style.visibility = 'hidden';
    mirror.style.whiteSpace = 'pre-wrap';
    mirror.style.wordWrap = 'break-word';
    mirror.style.left = '-9999px';
    mirror.style.top = '0';

    const selectionStart = el.selectionStart;
    mirror.textContent = el.value.substring(0, selectionStart);
    const marker = document.createElement('span');
    marker.textContent = el.value.substring(selectionStart) || '.';
    mirror.appendChild(marker);
    document.body.appendChild(mirror);

    const elRect = el.getBoundingClientRect();
    const mirrorRect = mirror.getBoundingClientRect();
    const markerRect = marker.getBoundingClientRect();

    const result = {
      top: elRect.top + (markerRect.top - mirrorRect.top) - el.scrollTop,
      left: elRect.left + (markerRect.left - mirrorRect.left) - el.scrollLeft,
      lineHeight: parseFloat(style.lineHeight) || 20,
    };

    document.body.removeChild(mirror);
    return result;
  },

  setCursor: function (el, position) {
    el.focus();
    el.setSelectionRange(position, position);
  },

  // Keeps ArrowUp/ArrowDown/Enter/Tab/Escape from doing their default
  // textarea thing (move caret, insert a newline) while the slash-command
  // menu is open. Blazor's own preventDefault modifier can't vary per key,
  // so this reads the menu-open state straight off a data attribute the
  // component keeps in sync on every render.
  attachSlashKeyGuard: function (el) {
    if (el.dataset.slashGuardAttached === 'true') {
      return;
    }
    el.dataset.slashGuardAttached = 'true';
    el.addEventListener('keydown', function (e) {
      if (el.dataset.slashMenuOpen !== 'true') {
        return;
      }
      if (['ArrowUp', 'ArrowDown', 'Enter', 'Tab', 'Escape'].indexOf(e.key) !== -1) {
        e.preventDefault();
      }
    });
  },

  // The preview pane's HTML is replaced wholesale on every render (Blazor
  // treats a MarkupString as opaque), so Prism's own DOMContentLoaded-based
  // auto-highlighting (disabled via `Prism.manual = true` in App.razor)
  // never sees the new <code> blocks. This re-scans the given container
  // after each render instead, then adds a copy button per code block -
  // both need redoing every time since the old nodes are gone.
  //
  // OnAfterRenderAsync calls this unconditionally on every render though, not
  // just ones where PreviewHtml actually changed (e.g. toggling ShowArchived
  // while the same note stays open) - when the markup is untouched, Blazor
  // doesn't recreate the <pre> nodes, so without the guard below each such
  // render would append yet another "Copy" button to the same block.
  highlightCode: function (el) {
    if (!el) {
      return;
    }
    if (window.Prism) {
      Prism.highlightAllUnder(el);
    }
    el.querySelectorAll('pre').forEach(function (pre) {
      if (pre.querySelector(':scope > .code-copy-btn')) {
        return;
      }
      var btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'code-copy-btn';
      btn.textContent = 'Copy';
      btn.addEventListener('click', function () {
        var code = pre.querySelector('code');
        var text = code ? code.innerText : pre.innerText;
        navigator.clipboard.writeText(text).then(function () {
          btn.textContent = 'Copied!';
          setTimeout(function () { btn.textContent = 'Copy'; }, 1500);
        });
      });
      pre.appendChild(btn);
    });
  },
};
