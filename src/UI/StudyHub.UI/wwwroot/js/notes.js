window.studyHubNotes = {
    highlightAll: function () {
        if (!window.Prism) {
            return;
        }

        document.querySelectorAll('.note-code-highlight code').forEach(function (element) {
            Prism.highlightElement(element);
        });
    },

    getCaretIndex: function (element) {
        return element ? element.selectionStart : 0;
    },

    focusAndSelect: function (element, start, end) {
        if (!element) {
            return;
        }

        element.focus();
        element.setSelectionRange(start, end);
    },

    // Measures where the caret currently sits inside a textarea by mirroring its text and box
    // model into an off-screen div, since browsers expose no direct API for caret pixel position.
    getCaretCoordinates: function (element) {
        const style = window.getComputedStyle(element);
        const mirror = document.createElement('div');
        const mirroredProperties = [
            'boxSizing', 'width', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
            'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth',
            'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 'letterSpacing', 'tabSize',
        ];

        mirror.style.position = 'absolute';
        mirror.style.visibility = 'hidden';
        mirror.style.whiteSpace = 'pre-wrap';
        mirror.style.wordWrap = 'break-word';
        mirror.style.top = '0';
        mirror.style.left = '-9999px';

        mirroredProperties.forEach(function (property) {
            mirror.style[property] = style[property];
        });

        const caretIndex = element.selectionStart;
        mirror.textContent = element.value.substring(0, caretIndex);

        const marker = document.createElement('span');
        marker.textContent = '​';
        mirror.appendChild(marker);
        document.body.appendChild(mirror);

        const lineHeight = parseFloat(style.lineHeight) || parseFloat(style.fontSize) * 1.2;
        const coordinates = {
            top: marker.offsetTop - element.scrollTop,
            left: marker.offsetLeft - element.scrollLeft,
            height: lineHeight,
        };

        document.body.removeChild(mirror);
        return coordinates;
    },

    // The palette's arrow/enter/escape/tab handling must run synchronously so the browser's
    // default caret movement and newline insertion never happen; a .NET round-trip can only
    // start after preventDefault(), so the "is the palette open" flag is tracked in JS too.
    attachPaletteKeyHandler: function (element, dotNetRef) {
        this.detachPaletteKeyHandler(element);

        const navigationKeys = ['ArrowUp', 'ArrowDown', 'Enter', 'Escape', 'Tab'];
        const handler = function (event) {
            if (!element._slashPaletteOpen || navigationKeys.indexOf(event.key) === -1) {
                return;
            }

            event.preventDefault();
            dotNetRef.invokeMethodAsync('OnPaletteKeyAsync', event.key);
        };

        element._slashPaletteHandler = handler;
        element.addEventListener('keydown', handler);
    },

    detachPaletteKeyHandler: function (element) {
        if (element && element._slashPaletteHandler) {
            element.removeEventListener('keydown', element._slashPaletteHandler);
            delete element._slashPaletteHandler;
        }
    },

    setPaletteOpen: function (element, isOpen) {
        if (element) {
            element._slashPaletteOpen = isOpen;
        }
    },
};
