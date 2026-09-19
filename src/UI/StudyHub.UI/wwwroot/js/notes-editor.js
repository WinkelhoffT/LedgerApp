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
};
