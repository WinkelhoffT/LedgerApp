window.studyHubNotes = {
    highlightAll: function () {
        if (!window.Prism) {
            return;
        }

        document.querySelectorAll('.note-code-highlight code').forEach(function (element) {
            Prism.highlightElement(element);
        });
    },
};
